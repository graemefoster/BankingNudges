import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useState, useEffect } from 'react';
import type { StaffSession } from '../types';

const navItems = [
  { to: '/customers', label: 'Customers' },
];

export default function Layout() {
  const navigate = useNavigate();
  const location = useLocation();
  const session = sessionStorage.getItem('staff_session');
  const staff: StaffSession | null = session ? JSON.parse(session) : null;
  const isLogin = !staff;
  const [virtualDate, setVirtualDate] = useState<string | null>(null);
  const [daysAdvanced, setDaysAdvanced] = useState(0);

  useEffect(() => {
    fetch('/api/time-travel/current')
      .then((r) => r.json())
      .then((data) => {
        setVirtualDate(data.virtualToday);
        setDaysAdvanced(data.daysAdvanced);
      })
      .catch(() => {});
  }, [location]);

  const handleLogout = () => {
    sessionStorage.removeItem('staff_session');
    navigate('/');
  };

  if (isLogin) {
    return (
      <div className="min-h-screen bg-crm-bg flex flex-col">
        {daysAdvanced > 0 && virtualDate && (
          <div className="bg-amber-500 text-white text-center text-xs py-1 font-medium">
            ⏰ Virtual Date: {virtualDate} ({daysAdvanced} days advanced)
          </div>
        )}
        <div className="bg-crm-dark text-white px-3 py-1.5 text-xs flex items-center justify-between">
          <span className="font-bold">Bank of Graeme — Staff CRM</span>
        </div>
        <main className="flex-1 flex items-center justify-center">
          <Outlet />
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-crm-bg flex flex-col">
      {daysAdvanced > 0 && virtualDate && (
        <div className="bg-amber-500 text-white text-center text-xs py-1 font-medium">
          ⏰ Virtual Date: {virtualDate} ({daysAdvanced} days advanced)
        </div>
      )}
      {/* Top bar */}
      <div className="bg-crm-dark text-white px-3 py-1.5 text-xs flex items-center justify-between">
        <div className="flex items-center gap-4">
          <span className="font-bold">Bank of Graeme — Staff CRM</span>
          <span className="text-white/40">|</span>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                isActive ? 'text-crm-accent underline' : 'text-white/70 hover:text-white'
              }
            >
              {item.label}
            </NavLink>
          ))}
        </div>
        <div className="flex items-center gap-3">
          <span className="text-white/60">
            {staff.displayName} ({staff.role})
          </span>
          <button onClick={handleLogout} className="text-white/50 hover:text-white underline">
            Logout
          </button>
        </div>
      </div>

      {/* Main content */}
      <main className="flex-1 overflow-y-auto p-3">
        <Outlet />
      </main>

      {/* Footer */}
      <div className="bg-crm-dark/10 text-text-secondary text-[10px] px-3 py-1 text-center border-t border-border">
        Bank of Graeme Internal CRM — For authorised staff use only
      </div>
    </div>
  );
}
