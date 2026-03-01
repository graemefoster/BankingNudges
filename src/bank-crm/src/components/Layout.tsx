import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import type { StaffSession } from '../types';

const navItems = [
  { to: '/customers', label: 'Customers' },
];

export default function Layout() {
  const navigate = useNavigate();
  const session = sessionStorage.getItem('staff_session');
  const staff: StaffSession | null = session ? JSON.parse(session) : null;
  const isLogin = !staff;

  const handleLogout = () => {
    sessionStorage.removeItem('staff_session');
    navigate('/');
  };

  if (isLogin) {
    return (
      <div className="min-h-screen bg-crm-bg flex flex-col">
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
