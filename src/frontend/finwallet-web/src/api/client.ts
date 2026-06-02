import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8081';

const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Attach JWT Token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('finwallet_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response Interceptor: Uniform Error Handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      const status = error.response.status;
      const data = error.response.data;

      // Handle 401 Unauthorized
      if (status === 401) {
        localStorage.removeItem('finwallet_token');
        localStorage.removeItem('finwallet_user');
        localStorage.removeItem('finwallet_role');
        // Redirect to login if not already there
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
      }

      // Throw standard API error structure
      if (data && data.errors && data.errors.length > 0) {
        return Promise.reject(data); // Returns the whole { success: false, message, errors }
      }
      
      return Promise.reject({
        success: false,
        message: data?.message || 'Request failed.',
        errors: [{ code: 'HTTP_ERROR', message: data?.title || error.message }]
      });
    }

    return Promise.reject({
      success: false,
      message: 'Server connection lost. Please verify backend is running.',
      errors: [{ code: 'NETWORK_ERROR', message: error.message }]
    });
  }
);

export default apiClient;
