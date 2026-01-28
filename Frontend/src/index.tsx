/**
 * @file index.tsx
 * @description Application entry point with React 19 and Redux Provider
 */

import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { store } from './store/store';
import '@heartlandone-private/fontawesome-pro/css/fontawesome.min.css';
import '@heartlandone-private/fontawesome-pro/css/regular.min.css';
import App from './App';
import './index.css';

const container = document.getElementById('root');
if (!container) {
  throw new Error('Root container not found');
}

const root = createRoot(container);
root.render(
  <Provider store={store}>
    <App />
  </Provider>
);
