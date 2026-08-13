/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Backend API kök adresi (sonunda / olmadan). */
  readonly VITE_API_BASE_URL: string;
  /** Uygulamanın sunulduğu alt dizin (örn. /GameFlow/). Kökte '/' kalır. */
  readonly VITE_BASE_PATH?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
