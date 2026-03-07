import { Link } from 'react-router-dom';
export default function NotFoundPage() {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen text-center">
      <h1 className="text-6xl font-bold text-blue mb-4">404</h1>
      <p className="text-subtext0 mb-6">Page not found</p>
      <Link to="/dashboard" className="bg-blue text-base px-4 py-2 rounded-lg">Go home</Link>
    </div>
  );
}
