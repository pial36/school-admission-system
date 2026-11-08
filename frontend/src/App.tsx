import { ApplicationView } from './components/ApplicationView';
import { ClassList } from './components/ClassList';
import { CreateClass } from './components/CreateClass';
import { MyApplications } from './components/MyApplications';
import { StudentBar } from './components/StudentBar';
import { useHashRoute, useStudentId } from './hooks';

const NAV = [
  { path: '/classes', label: 'Classes' },
  { path: '/my', label: 'My applications' },
  { path: '/admin', label: 'Create class' },
];

export default function App() {
  const route = useHashRoute();
  const [studentId, setStudentId] = useStudentId();
  const holdMatch = route.match(/^\/holds\/([^/]+)$/);

  let page;
  if (holdMatch) page = <ApplicationView holdId={decodeURIComponent(holdMatch[1])} studentId={studentId} />;
  else if (route === '/my') page = <MyApplications studentId={studentId} />;
  else if (route === '/admin') page = <CreateClass />;
  else page = <ClassList studentId={studentId} />;

  return (
    <div className="app">
      <header className="header">
        <h1>🏫 School Admission</h1>
        <nav>
          {NAV.map((n) => (
            <a key={n.path} href={`#${n.path}`}
               className={route === n.path || (n.path === '/my' && holdMatch) ? 'active' : ''}>
              {n.label}
            </a>
          ))}
        </nav>
      </header>
      <StudentBar studentId={studentId} onChange={setStudentId} />
      <main>{page}</main>
      <footer className="muted small">First come, first served · seats are held for a limited time until the fee is paid</footer>
    </div>
  );
}
