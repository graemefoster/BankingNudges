import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Customer } from '../types';
import { getCustomers } from '../api/bankApi';

export default function LoginPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const pageSize = 20;
  const navigate = useNavigate();

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    getCustomers(search || undefined, page, pageSize, controller.signal)
      .then((data) => {
        setCustomers(data.customers);
        setTotal(data.total);
      })
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      })
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [search, page]);

  function selectCustomer(c: Customer) {
    sessionStorage.setItem('customerId', c.id);
    sessionStorage.setItem('customerName', c.fullName);
    navigate('/dashboard');
  }

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  return (
    <div className="flex flex-col items-center pt-12">
      {/* Logo icon */}
      <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-accent-teal to-accent-cyan flex items-center justify-center mb-5">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <path d="M3 21h18" />
          <path d="M3 10h18" />
          <path d="M5 6l7-3 7 3" />
          <path d="M4 10v11" />
          <path d="M20 10v11" />
          <path d="M8 14v4" />
          <path d="M12 14v4" />
          <path d="M16 14v4" />
        </svg>
      </div>
      <h2 className="text-2xl font-bold bg-gradient-to-r from-accent-teal to-accent-cyan bg-clip-text text-transparent mb-1">
        Bank of Graeme
      </h2>
      <p className="text-text-secondary text-sm mb-4">Select your account to continue</p>

      {/* Search */}
      <div className="w-full mb-4">
        <input
          type="text"
          placeholder="Search by name or email…"
          value={search}
          onChange={(e) => handleSearchChange(e.target.value)}
          className="w-full bg-dark-elevated border border-border rounded-xl px-4 py-3 text-sm text-text-primary placeholder-text-secondary focus:outline-none focus:border-accent-teal transition-colors"
        />
      </div>

      {error && (
        <div className="w-full bg-accent-coral/10 border border-accent-coral/30 text-accent-coral rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <>
          <p className="w-full text-xs text-text-secondary mb-2">{total} customer{total !== 1 ? 's' : ''} found</p>
          <div className="w-full space-y-3">
            {customers.map((c) => (
              <button
                key={c.id}
                onClick={() => selectCustomer(c)}
                className="w-full bg-dark-elevated hover:bg-dark-elevated/80 rounded-xl p-4 text-left transition-colors border border-border"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-semibold text-text-primary">{c.fullName}</p>
                    <p className="text-xs text-text-secondary mt-0.5">{c.email}</p>
                  </div>
                  <div className="text-right flex flex-col items-end gap-1">
                    {c.persona && (
                      <span className="text-xs bg-accent-amber/15 text-accent-amber px-2 py-0.5 rounded-full font-medium">
                        {c.persona}
                      </span>
                    )}
                    <span className="text-xs bg-accent-teal/15 text-accent-teal px-2 py-1 rounded-full font-medium">
                      {c.accountCount} account{c.accountCount !== 1 ? 's' : ''}
                    </span>
                  </div>
                </div>
              </button>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center gap-3 mt-6 mb-4">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 text-sm rounded-lg bg-dark-elevated border border-border text-text-primary disabled:opacity-40 disabled:cursor-not-allowed hover:bg-dark-elevated/80 transition-colors"
              >
                ← Prev
              </button>
              <span className="text-xs text-text-secondary">
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1.5 text-sm rounded-lg bg-dark-elevated border border-border text-text-primary disabled:opacity-40 disabled:cursor-not-allowed hover:bg-dark-elevated/80 transition-colors"
              >
                Next →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
