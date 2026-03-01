import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getCustomer, updateCustomer, getNotes, addNote } from '../api/crmApi';
import type { CustomerDetail, CustomerNote } from '../types';
import { formatDate, formatDateTime, formatCurrency, accountTypeLabel, accountTypeBadge } from '../types';

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
    return <div className="text-center py-12 text-text-secondary">Loading...</div>;
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-3 mb-6">
        <button
          onClick={() => navigate('/customers')}
          className="text-crm-secondary hover:text-crm-dark transition-colors"
        >
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-crm-dark">{customer.firstName} {customer.lastName}</h1>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-crm-warning/10 text-crm-warning text-sm rounded-lg">{error}</div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Customer details */}
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-crm-card rounded-xl shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-crm-dark">Details</h2>
              {!editing ? (
                <button
                  onClick={() => setEditing(true)}
                  className="text-sm text-crm-accent hover:text-crm-secondary transition-colors font-medium"
                >
                  Edit
                </button>
              ) : (
                <div className="flex gap-2">
                  <button
                    onClick={() => setEditing(false)}
                    className="text-sm text-text-secondary hover:text-text-primary transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleSave}
                    disabled={saving}
                    className="text-sm bg-crm-accent text-crm-dark px-3 py-1 rounded-lg font-medium hover:bg-crm-accent/90 disabled:opacity-50"
                  >
                    {saving ? 'Saving...' : 'Save'}
                  </button>
                </div>
              )}
            </div>

            {editing ? (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-text-secondary mb-1">First Name</label>
                  <input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-secondary mb-1">Last Name</label>
                  <input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-secondary mb-1">Email</label>
                  <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-secondary mb-1">Phone</label>
                  <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-text-secondary mb-1">Date of Birth</label>
                  <input type="date" value={form.dateOfBirth} onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent" />
                </div>
              </div>
            ) : (
              <dl className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <dt className="text-text-secondary text-xs">Email</dt>
                  <dd className="font-medium">{customer.email}</dd>
                </div>
                <div>
                  <dt className="text-text-secondary text-xs">Phone</dt>
                  <dd className="font-medium">{customer.phone || '—'}</dd>
                </div>
                <div>
                  <dt className="text-text-secondary text-xs">Date of Birth</dt>
                  <dd className="font-medium">{formatDate(customer.dateOfBirth)}</dd>
                </div>
                <div>
                  <dt className="text-text-secondary text-xs">Customer Since</dt>
                  <dd className="font-medium">{formatDate(customer.createdAt)}</dd>
                </div>
              </dl>
            )}
          </div>

          {/* Accounts */}
          <div className="bg-crm-card rounded-xl shadow-sm p-6">
            <h2 className="text-lg font-semibold text-crm-dark mb-4">Accounts</h2>
            <div className="space-y-3">
              {customer.accounts.map((a) => (
                <div
                  key={a.id}
                  onClick={() => navigate(`/accounts/${a.id}`)}
                  className="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:bg-crm-card-hover cursor-pointer transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${accountTypeBadge[a.accountType]}`}>
                      {accountTypeLabel[a.accountType]}
                    </span>
                    <span className="text-sm font-medium">{a.name}</span>
                    {!a.isActive && (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-gray-200 text-gray-600 font-medium">Closed</span>
                    )}
                  </div>
                  <span className={`text-sm font-semibold ${a.balance < 0 ? 'text-crm-warning' : 'text-crm-dark'}`}>
                    {formatCurrency(a.balance)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Notes sidebar */}
        <div className="space-y-6">
          <div className="bg-crm-card rounded-xl shadow-sm p-6">
            <h2 className="text-lg font-semibold text-crm-dark mb-4">Notes</h2>

            {/* Add note */}
            <div className="mb-4">
              <textarea
                value={newNote}
                onChange={(e) => setNewNote(e.target.value)}
                placeholder="Add a note..."
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent resize-none"
              />
              <button
                onClick={handleAddNote}
                disabled={!newNote.trim()}
                className="mt-2 w-full py-2 bg-crm-accent text-crm-dark text-sm font-medium rounded-lg hover:bg-crm-accent/90 disabled:opacity-40 transition-colors"
              >
                Add Note
              </button>
            </div>

            {/* Notes list */}
            <div className="space-y-3">
              {notes.length === 0 ? (
                <p className="text-sm text-text-secondary text-center py-4">No notes yet</p>
              ) : (
                notes.map((n) => (
                  <div key={n.id} className="p-3 bg-crm-bg rounded-lg">
                    <p className="text-sm">{n.content}</p>
                    <div className="mt-2 flex items-center justify-between text-xs text-text-secondary">
                      <span>{n.author}</span>
                      <span>{formatDateTime(n.createdAt)}</span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
