import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getCustomer, updateCustomer, getNotes, addNote } from '../api/crmApi';
import type { CustomerDetail, CustomerNote } from '../types';
import { formatDate, formatDateTime, formatCurrency, accountTypeLabel } from '../types';

export default function CustomerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const customerId = Number(id);

  const [customer, setCustomer] = useState<CustomerDetail | null>(null);
  const [notes, setNotes] = useState<CustomerNote[]>([]);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', phone: '', dateOfBirth: '' });
  const [newNote, setNewNote] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    try {
      const [c, n] = await Promise.all([getCustomer(customerId), getNotes(customerId)]);
      setCustomer(c);
      setNotes(n);
      setForm({
        firstName: c.firstName,
        lastName: c.lastName,
        email: c.email,
        phone: c.phone || '',
        dateOfBirth: c.dateOfBirth,
      });
    } catch (err) {
      console.error('Failed to load customer', err);
    }
  }, [customerId]);

  useEffect(() => { load(); }, [load]);

  const handleSave = async () => {
    setSaving(true);
    setError('');
    try {
      await updateCustomer(customerId, {
        ...form,
        phone: form.phone || null,
      });
      setEditing(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  const handleAddNote = async () => {
    if (!newNote.trim()) return;
    try {
      const note = await addNote(customerId, newNote.trim());
      setNotes((prev) => [note, ...prev]);
      setNewNote('');
    } catch (err) {
      console.error('Failed to add note', err);
    }
  };

  if (!customer) {
    return <div className="py-4 text-text-secondary text-xs">Loading...</div>;
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-2 mb-2">
        <button onClick={() => navigate('/customers')} className="text-xs text-crm-dark underline hover:no-underline cursor-pointer">
          &laquo; Back to list
        </button>
        <span className="text-text-secondary">|</span>
        <span className="text-sm font-bold">{customer.firstName} {customer.lastName}</span>
        <span className="text-xs text-text-secondary">(ID: {customer.id})</span>
        {customer.persona && (
          <span className="inline-block px-1.5 py-0.5 rounded bg-crm-highlight/30 text-crm-dark text-[10px] font-medium">
            {customer.persona}
          </span>
        )}
      </div>

      {error && (
        <div className="mb-2 p-1.5 bg-red-100 text-red-700 text-xs border border-red-300">{error}</div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
        {/* Left column */}
        <div className="lg:col-span-2 space-y-3">
          {/* Customer details */}
          <fieldset>
            <legend>Customer Details</legend>
            <div className="flex justify-end mb-2">
              {!editing ? (
                <button onClick={() => setEditing(true)} className="text-xs text-crm-dark underline hover:no-underline cursor-pointer">
                  [Edit]
                </button>
              ) : (
                <div className="flex gap-2">
                  <button onClick={() => setEditing(false)} className="text-xs text-text-secondary underline hover:no-underline cursor-pointer">
                    [Cancel]
                  </button>
                  <button onClick={handleSave} disabled={saving} className="text-xs text-crm-dark underline hover:no-underline disabled:opacity-50 cursor-pointer">
                    {saving ? '[Saving...]' : '[Save]'}
                  </button>
                </div>
              )}
            </div>

            {editing ? (
              <table className="text-xs bg-white">
                <tbody>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold w-32">First Name</td>
                    <td className="px-2 py-1">
                      <input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                        className="w-full px-1 py-0.5 border border-border text-xs" />
                    </td>
                  </tr>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold">Last Name</td>
                    <td className="px-2 py-1">
                      <input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                        className="w-full px-1 py-0.5 border border-border text-xs" />
                    </td>
                  </tr>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold">Email</td>
                    <td className="px-2 py-1">
                      <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })}
                        className="w-full px-1 py-0.5 border border-border text-xs" />
                    </td>
                  </tr>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold">Phone</td>
                    <td className="px-2 py-1">
                      <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })}
                        className="w-full px-1 py-0.5 border border-border text-xs" />
                    </td>
                  </tr>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold">Date of Birth</td>
                    <td className="px-2 py-1">
                      <input type="date" value={form.dateOfBirth} onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
                        className="px-1 py-0.5 border border-border text-xs" />
                    </td>
                  </tr>
                </tbody>
              </table>
            ) : (
              <table className="text-xs bg-white">
                <tbody>
                  <tr><td className="px-2 py-1 bg-gray-50 font-bold w-32">Email</td><td className="px-2 py-1">{customer.email}</td></tr>
                  <tr><td className="px-2 py-1 bg-gray-50 font-bold">Phone</td><td className="px-2 py-1">{customer.phone || '—'}</td></tr>
                  <tr><td className="px-2 py-1 bg-gray-50 font-bold">Date of Birth</td><td className="px-2 py-1">{formatDate(customer.dateOfBirth)}</td></tr>
                  <tr><td className="px-2 py-1 bg-gray-50 font-bold">Customer Since</td><td className="px-2 py-1">{formatDate(customer.createdAt)}</td></tr>
                </tbody>
              </table>
            )}
          </fieldset>

          {/* Accounts table */}
          <fieldset>
            <legend>Accounts</legend>
            <table className="w-full text-xs bg-white">
              <thead>
                <tr className="bg-crm-dark text-white text-left">
                  <th className="px-2 py-1 font-normal">ID</th>
                  <th className="px-2 py-1 font-normal">Name</th>
                  <th className="px-2 py-1 font-normal">Type</th>
                  <th className="px-2 py-1 font-normal text-right">Balance</th>
                  <th className="px-2 py-1 font-normal text-center">Status</th>
                </tr>
              </thead>
              <tbody>
                {customer.accounts.map((a, i) => (
                  <tr
                    key={a.id}
                    onClick={() => navigate(`/accounts/${a.id}`)}
                    className={`cursor-pointer hover:bg-crm-highlight/30 ${i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}`}
                  >
                    <td className="px-2 py-1 text-text-secondary">{a.id}</td>
                    <td className="px-2 py-1 font-bold text-crm-dark underline">{a.name}</td>
                    <td className="px-2 py-1">{accountTypeLabel[a.accountType]}</td>
                    <td className={`px-2 py-1 text-right font-mono ${a.balance < 0 ? 'text-red-700' : ''}`}>
                      {formatCurrency(a.balance)}
                    </td>
                    <td className="px-2 py-1 text-center">
                      {a.isActive
                        ? <span className="text-crm-secondary">Active</span>
                        : <span className="text-red-600 font-bold">CLOSED</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </fieldset>
        </div>

        {/* Notes column */}
        <div>
          <fieldset>
            <legend>Notes</legend>

            {/* Add note */}
            <div className="mb-2">
              <textarea
                value={newNote}
                onChange={(e) => setNewNote(e.target.value)}
                placeholder="Add a note..."
                rows={3}
                className="w-full px-2 py-1 border border-border text-xs resize-none"
              />
              <button
                onClick={handleAddNote}
                disabled={!newNote.trim()}
                className="mt-1 px-3 py-1 bg-crm-dark text-white text-xs font-bold border border-crm-dark hover:bg-crm-dark/90 disabled:opacity-40 cursor-pointer"
              >
                Add Note
              </button>
            </div>

            {/* Notes list */}
            <div className="space-y-1 max-h-80 overflow-y-auto">
              {notes.length === 0 ? (
                <p className="text-xs text-text-secondary py-2">No notes on file.</p>
              ) : (
                notes.map((n) => (
                  <div key={n.id} className="border border-border bg-white p-1.5 text-xs">
                    <p>{n.content}</p>
                    <div className="mt-1 text-[10px] text-text-secondary">
                      — {n.author}, {formatDateTime(n.createdAt)}
                    </div>
                  </div>
                ))
              )}
            </div>
          </fieldset>
        </div>
      </div>
    </div>
  );
}
