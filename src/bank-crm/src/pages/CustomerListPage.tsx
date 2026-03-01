import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { getCustomers } from '../api/crmApi';
import type { Customer } from '../types';
import { formatDate } from '../types';

export default function CustomerListPage() {
  const navigate = useNavigate();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [total, setTotal] = useState(0);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getCustomers(search || undefined, page);
      setCustomers(data.customers);
      setTotal(data.total);
    } catch (err) {
      console.error('Failed to load customers', err);
    } finally {
      setLoading(false);
    }
  }, [search, page]);

  useEffect(() => { load(); }, [load]);

  const totalPages = Math.ceil(total / 20);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-crm-dark">Customers</h1>
        <span className="text-sm text-text-secondary">{total} total</span>
      </div>

      {/* Search */}
      <div className="mb-4">
        <input
          type="text"
          placeholder="Search by name, email, or phone..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="w-full px-4 py-2.5 bg-crm-card border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-crm-accent focus:border-transparent text-sm"
        />
      </div>

      {/* Table */}
      <div className="bg-crm-card rounded-xl shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-crm-dark/5 text-left">
              <th className="px-4 py-3 font-medium text-text-secondary">Name</th>
              <th className="px-4 py-3 font-medium text-text-secondary">Email</th>
              <th className="px-4 py-3 font-medium text-text-secondary">Phone</th>
              <th className="px-4 py-3 font-medium text-text-secondary text-center">Accounts</th>
              <th className="px-4 py-3 font-medium text-text-secondary">Since</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-text-secondary">
                  Loading...
                </td>
              </tr>
            ) : customers.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-text-secondary">
                  No customers found
                </td>
              </tr>
            ) : (
              customers.map((c) => (
                <tr
                  key={c.id}
                  onClick={() => navigate(`/customers/${c.id}`)}
                  className="border-t border-gray-100 hover:bg-crm-card-hover cursor-pointer transition-colors"
                >
                  <td className="px-4 py-3 font-medium">{c.fullName}</td>
                  <td className="px-4 py-3 text-text-secondary">{c.email}</td>
                  <td className="px-4 py-3 text-text-secondary">{c.phone || '—'}</td>
                  <td className="px-4 py-3 text-center">
                    <span className="inline-flex items-center justify-center w-7 h-7 bg-crm-accent/15 text-crm-dark text-xs font-medium rounded-full">
                      {c.activeAccountCount ?? c.accountCount}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-text-secondary">{formatDate(c.createdAt)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 mt-4">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-3 py-1.5 text-sm bg-crm-card border border-gray-200 rounded-lg hover:bg-crm-card-hover disabled:opacity-40"
          >
            Previous
          </button>
          <span className="text-sm text-text-secondary">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="px-3 py-1.5 text-sm bg-crm-card border border-gray-200 rounded-lg hover:bg-crm-card-hover disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
