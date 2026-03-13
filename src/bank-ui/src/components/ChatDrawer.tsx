import { useEffect, useRef, useState, useCallback } from 'react';
import type { ChatMessage, NudgeDto } from '../types';
import { startChatSession, sendChatMessage, deleteChatSession } from '../api/bankApi';

interface Props {
  nudge: NudgeDto;
  customerId: number;
  onClose: () => void;
}

export default function ChatDrawer({ nudge, customerId, onClose }: Props) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [streaming, setStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [initializing, setInitializing] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Initialize chat session on mount
  useEffect(() => {
    let cancelled = false;

    async function init() {
      try {
        const session = await startChatSession(customerId, nudge.id);
        if (!cancelled) {
          setSessionId(session.sessionId);
          setInitializing(false);
          inputRef.current?.focus();
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : 'Failed to start chat');
          setInitializing(false);
        }
      }
    }

    init();
    return () => {
      cancelled = true;
    };
  }, [customerId, nudge.id]);

  // Clean up session on unmount
  useEffect(() => {
    return () => {
      if (sessionId) {
        deleteChatSession(sessionId).catch(() => {});
      }
    };
  }, [sessionId]);

  const sendMessage = useCallback(async () => {
    const text = input.trim();
    if (!text || !sessionId || streaming) return;

    setInput('');
    setError(null);
    setMessages(prev => [...prev, { role: 'user', content: text }]);
    setStreaming(true);

    // Add placeholder for assistant response
    setMessages(prev => [...prev, { role: 'assistant', content: '' }]);

    try {
      for await (const chunk of sendChatMessage(sessionId, text)) {
        setMessages(prev => {
          const updated = [...prev];
          const last = updated[updated.length - 1];
          if (last?.role === 'assistant') {
            updated[updated.length - 1] = { ...last, content: last.content + chunk };
          }
          return updated;
        });
      }
    } catch (e) {
      setMessages(prev => {
        const updated = [...prev];
        const last = updated[updated.length - 1];
        if (last?.role === 'assistant' && last.content === '') {
          updated.pop();
        }
        return updated;
      });
      setError(e instanceof Error ? e.message : 'Something went wrong');
    } finally {
      setStreaming(false);
      inputRef.current?.focus();
    }
  }, [input, sessionId, streaming]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex flex-col">
      {/* Backdrop */}
      <div className="flex-1 bg-black/80 backdrop-blur-md" onClick={onClose} />

      {/* Drawer */}
      <div className="bg-dark-base border-t border-border rounded-t-2xl flex flex-col max-h-[75vh] animate-slide-up">
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-border">
          <div className="flex items-center gap-2">
            <span className="text-base">💬</span>
            <h3 className="text-sm font-semibold text-text-primary">Chat about this insight</h3>
          </div>
          <button
            onClick={onClose}
            className="w-7 h-7 flex items-center justify-center rounded-full bg-dark-elevated text-text-secondary hover:text-text-primary transition-colors"
          >
            ✕
          </button>
        </div>

        {/* Nudge context card */}
        <div className="px-4 py-2 border-b border-border/50">
          <div className="bg-dark-elevated rounded-lg px-3 py-2 text-xs text-text-secondary">
            <span className="text-text-muted">Insight: </span>
            {nudge.message}
          </div>
        </div>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3 min-h-[200px]">
          {initializing && (
            <div className="flex items-center justify-center gap-2 py-8">
              <div className="w-4 h-4 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
              <span className="text-xs text-text-secondary">Starting chat...</span>
            </div>
          )}

          {messages.map((msg, i) => (
            <div
              key={i}
              className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
            >
              <div
                className={`max-w-[80%] rounded-2xl px-3 py-2 text-sm leading-relaxed ${
                  msg.role === 'user'
                    ? 'bg-accent-teal text-white rounded-br-md'
                    : 'bg-dark-elevated text-text-primary rounded-bl-md'
                }`}
              >
                {msg.content || (
                  <div className="flex items-center gap-1.5">
                    <div className="w-1.5 h-1.5 bg-accent-teal rounded-full animate-bounce" />
                    <div className="w-1.5 h-1.5 bg-accent-teal rounded-full animate-bounce [animation-delay:0.15s]" />
                    <div className="w-1.5 h-1.5 bg-accent-teal rounded-full animate-bounce [animation-delay:0.3s]" />
                  </div>
                )}
              </div>
            </div>
          ))}

          {error && (
            <div className="text-center text-xs text-accent-coral bg-accent-coral/10 rounded-lg px-3 py-2">
              {error}
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* Input bar */}
        <div className="px-4 py-3 border-t border-border">
          <div className="flex gap-2">
            <input
              ref={inputRef}
              type="text"
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={initializing ? 'Starting...' : 'Ask about this insight...'}
              disabled={initializing || streaming || !sessionId}
              className="flex-1 bg-dark-elevated border border-border rounded-xl px-3 py-2 text-sm text-text-primary placeholder:text-text-muted/50 focus:outline-none focus:border-accent-teal disabled:opacity-50"
            />
            <button
              onClick={sendMessage}
              disabled={!input.trim() || streaming || !sessionId}
              className="bg-accent-teal text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-accent-teal/90 transition-colors disabled:opacity-30"
            >
              {streaming ? '...' : '→'}
            </button>
          </div>
          <p className="text-[10px] text-text-muted/40 mt-1.5 text-center">
            General information only — not personal financial advice.
          </p>
        </div>
      </div>
    </div>
  );
}
