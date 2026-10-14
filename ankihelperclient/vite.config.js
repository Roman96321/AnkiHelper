import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'node:fs';

const httpsCertificatePath = process.env.VITE_HTTPS_CERT_PATH;
const httpsCertificatePassword = process.env.VITE_HTTPS_CERT_PASSWORD;
const useHttps = process.env.VITE_USE_HTTPS !== 'false';

const httpsOptions = useHttps
    ? httpsCertificatePath && fs.existsSync(httpsCertificatePath)
        ? {
            pfx: fs.readFileSync(httpsCertificatePath),
            passphrase: httpsCertificatePassword
        }
        : true
    : false;

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        port: 56573,
        strictPort: true,
        https: httpsOptions,
    }
})
