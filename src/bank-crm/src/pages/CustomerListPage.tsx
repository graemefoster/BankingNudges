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
      <div className="flex items-center justify-between mb-2">
        <h2 className="text-sm font-bold uppercase text-text-secondary">Customer Search</h2>
        <span className="text-xs text-text-secondary">{total} record(s)</span>
      </div>

      {/* Search */}
      <div className="mb-2">
        <input
          type="text"
          placeholder="Search by name, email, or phone..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="w-full px-2 py-1 border border-border bg-white text-xs"
        />
      </div>

      {/* Table */}
      <table className="w-full text-xs bg-white">
        <thead>
          <tr className="bg-crm-dark text-white text-left">
            <th className="px-2 py-1 font-normal">ID</th>
            <th className="px-2 py-1 font-normal">Name</th>
            <th className="px-2 py-1 font-normal">Email</th>
            <th className="px-2 py-1 font-normal">Phone</th>
            <th className="px-2 py-1 font-normal text-center">Accts</th>
            <th className="px-2 py-1 font-normal">Created</th>
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr>
              <td colSpan={6} className="px-2 py-4 text-center text-text-secondary">
                Loading...
              </td>
            </tr>
          ) : customers.length === 0 ? (
            <tr>
              <td colSpan={6} className="px-2 py-4 text-center text-text-secondary">
                No customers found
              </td>
            </tr>
          ) : (
            customers.map((c, i) => (
              <tr
                key={c.id}
                onClick={() => navigate(`/customers/${c.id}`)}
                className={`cursor-pointer hover:bg-crm-highlight/30 ${i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}`}
              >
                <td className="px-2 py-1 text-text-secondary">{c.id}</td>
                <td className="px-2 py-1 font-bold text-crm-dark underline">{c.fullName}</td>
                <td className="px-2 py-1">{c.email}</td>
                <td className="px-2 py-1">{c.phone || '—'}</td>
                <td className="px-2 py-1 text-center">{c.activeAccountCount ?? c.accountCount}</td>
                <td className="px-2 py-1 text-text-secondary">{formatDate(c.createdAt)}</td>
              </tr>
            ))
          )}
        </tbody>
      </table>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center gap-2 mt-2 text-xs">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-2 py-0.5 bg-white border border-border hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            &laquo; Prev
          </button>
          <span className="text-text-secondary">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="px-2 py-0.5 bg-white border border-border hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            Next &raquo;
          </button>
        </div>
      )}
    </div>
  );
}
