import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import type { StaffSession } from '../types';

const navItems = [
  { to: '/customers', label: 'Customers', icon: '👥' },
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
        <header className="bg-crm-dark px-6 py-4 shadow-md">
          <h1 className="text-lg font-bold text-white">🏦 Bank of Graeme — Staff CRM</h1>
        </header>
        <main className="flex-1 flex items-center justify-center">
          <Outlet />
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-crm-bg flex">
      {/* Sidebar */}
      <aside className="w-56 bg-crm-dark text-white flex flex-col shrink-0">
        <div className="px-4 py-5 border-b border-white/10">
          <h1 className="text-base font-bold">🏦 Staff CRM</h1>
          <p className="text-xs text-white/60 mt-1">Bank of Graeme</p>
        </div>
        <nav className="flex-1 py-3">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-4 py-2.5 text-sm transition-colors ${
                  isActive
                    ? 'bg-white/10 text-crm-accent font-medium'
                    : 'text-white/70 hover:bg-white/5 hover:text-white'
                }`
              }
            >
              <span>{item.icon}</span>
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="px-4 py-4 border-t border-white/10">
          <p className="text-xs text-white/60">{staff.displayName}</p>
          <p className="text-xs text-white/40 capitalize">{staff.role}</p>
          <button
            onClick={handleLogout}
            className="mt-2 text-xs text-crm-warning hover:text-crm-warning/80 transition-colors"
          >
            Sign out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-y-auto">
        <div className="max-w-5xl mx-auto px-6 py-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
