import type { Account, Customer, Transaction, ScheduledPayment, NudgeInsightResponse, NudgeHistoryItem, TransactionFilters, ActiveHoliday } from '../types';

const BASE = '/api';

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, init);
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(text || `Request failed: ${res.status}`);
  }
  const text = await res.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
}

export interface CustomerPage {
  customers: Customer[];
  total: number;
  page: number;
  pageSize: number;
}

export function getCustomers(search?: string, page = 1, pageSize = 20, signal?: AbortSignal): Promise<CustomerPage> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (search) params.set('search', search);
  return fetchJson<CustomerPage>(`${BASE}/customers?${params}`, signal ? { signal } : undefined);
}

export function getCustomerAccounts(customerId: string, includeInactive = false): Promise<Account[]> {
  const params = new URLSearchParams();
  if (includeInactive) params.set('includeInactive', 'true');
  const qs = params.toString();
  const url = qs
    ? `${BASE}/customers/${customerId}/accounts?${qs}`
    : `${BASE}/customers/${customerId}/accounts`;
  return fetchJson<Account[]>(url);
}

export function getAccount(accountId: string): Promise<Account> {
  return fetchJson<Account>(`${BASE}/accounts/${accountId}`);
}

export function getTransactions(
  accountId: string,
  page = 1,
  pageSize = 20,
  filters?: TransactionFilters,
): Promise<Transaction[]> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (filters?.search) params.set('search', filters.search);
  if (filters?.category) params.set('category', filters.category);
  if (filters?.from) params.set('from', filters.from);
  if (filters?.to) params.set('to', filters.to);
  return fetchJson<Transaction[]>(
    `${BASE}/accounts/${accountId}/transactions?${params}`,
  );
}

export function deposit(accountId: string, amount: number, description: string) {
  return fetchJson<void>(`${BASE}/accounts/${accountId}/deposit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ amount, description }),
  });
}

export function withdraw(accountId: string, amount: number, description: string) {
  return fetchJson<void>(`${BASE}/accounts/${accountId}/withdraw`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ amount, description }),
  });
}

export function transfer(
  fromAccountId: string,
  toAccountId: string,
  amount: number,
  description: string,
) {
  return fetchJson<void>(`${BASE}/transfers`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fromAccountId, toAccountId, amount, description }),
  });
}

export function lookupAccount(bsb: string, accountNumber: string) {
  return fetchJson<import('../types').PayeeLookup>(
    `${BASE}/payments/lookup?bsb=${encodeURIComponent(bsb)}&accountNumber=${encodeURIComponent(accountNumber)}`,
  );
}

export function pay(
  callerCustomerId: string,
  fromAccountId: string,
  toBsb: string,
  toAccountNumber: string,
  amount: number,
  description: string,
  reference: string,
) {
  return fetchJson<void>(`${BASE}/payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ callerCustomerId: Number(callerCustomerId), fromAccountId: Number(fromAccountId), toBsb, toAccountNumber, amount, description, reference }),
  });
}

export function getScheduledPayments(accountId: string): Promise<ScheduledPayment[]> {
  return fetchJson<ScheduledPayment[]>(`${BASE}/accounts/${accountId}/scheduled-payments`);
}

export function createScheduledPayment(data: {
  accountId: number;
  payeeName: string;
  payeeBsb?: string;
  payeeAccountNumber?: string;
  payeeAccountId?: number;
  amount: number;
  description?: string;
  reference?: string;
  frequency: string;
  startDate: string;
  endDate?: string;
}) {
  return fetchJson<ScheduledPayment>(`${BASE}/scheduled-payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
}

export function cancelScheduledPayment(id: number) {
  return fetchJson<{ message: string }>(`${BASE}/scheduled-payments/${id}`, {
    method: 'DELETE',
  });
}

export function generateNudge(customerId: string): Promise<import('../types').NudgeGenerateResult> {
  return fetchJson<import('../types').NudgeGenerateResult>(`${BASE}/nudges/generate/${customerId}`, {
    method: 'POST',
  });
}

export function respondToNudge(nudgeId: number, action: string): Promise<void> {
  return fetchJson<void>(`${BASE}/nudges/${nudgeId}/respond`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ action }),
  });
}

export function getNudgeInsight(nudgeId: number): Promise<NudgeInsightResponse> {
  return fetchJson<NudgeInsightResponse>(`${BASE}/nudges/${nudgeId}/insight`);
}

export function getNudgeHistory(customerId: string): Promise<NudgeHistoryItem[]> {
  return fetchJson<NudgeHistoryItem[]>(`${BASE}/nudges/${customerId}/history`);
}

export async function getActiveHoliday(customerId: string): Promise<ActiveHoliday | null> {
  const res = await fetch(`${BASE}/customers/${customerId}/holidays/active`);
  if (res.status === 204 || res.status === 404) return null;
  if (!res.ok) throw new Error(`Request failed: ${res.status}`);
  return res.json() as Promise<ActiveHoliday>;
}

export function startChatSession(customerId: number, nudgeId: number): Promise<import('../types').ChatSession> {
  return fetchJson<import('../types').ChatSession>(`${BASE}/chat/nudge/start`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ customerId, nudgeId }),
  });
}

export async function* sendChatMessage(sessionId: string, message: string): AsyncGenerator<string> {
  const res = await fetch(`${BASE}/chat/nudge/message`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sessionId, message }),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(text || `Chat request failed: ${res.status}`);
  }

  const reader = res.body?.getReader();
  if (!reader) throw new Error('No response body');

  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });

    // Parse SSE lines
    const lines = buffer.split('\n');
    buffer = lines.pop() ?? '';

    for (const line of lines) {
      if (!line.startsWith('data: ')) continue;
      const data = line.slice(6).trim();
      if (data === '[DONE]') return;

      try {
        const parsed = JSON.parse(data) as { content: string };
        if (parsed.content) yield parsed.content;
      } catch {
        // Skip unparseable chunks
      }
    }
  }
}

export function deleteChatSession(sessionId: string): Promise<void> {
  return fetchJson<void>(`${BASE}/chat/nudge/${sessionId}`, { method: 'DELETE' });
}
