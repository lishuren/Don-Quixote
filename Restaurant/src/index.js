import { createRoot } from 'react-dom/client';
import './index.css';
import '@heartlandone-private/fontawesome-pro/css/fontawesome.min.css';
import '@heartlandone-private/fontawesome-pro/css/regular.min.css';
import App from './App';

const container = document.getElementById('root');
const root = createRoot(container);
root.render(<App />);
