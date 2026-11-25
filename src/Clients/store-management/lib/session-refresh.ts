import { createHash } from "node:crypto";

// Coalesce the parallel requests made by dashboard loading and React Strict Mode.
// Retain a successful rotation briefly so requests carrying the old cookie receive
// the SAME successor, never a second rotation. No plaintext token is used as a key.
export function createRefreshCoordinator<T>(retentionMs = 10_000) {
  const pending = new Map<string, Promise<T>>();
  return (credential: string, refresh: () => Promise<T>): Promise<T> => {
    const key = createHash("sha256").update(credential).digest("hex");
    const current = pending.get(key);
    if (current) return current;
    const result = Promise.resolve().then(refresh);
    pending.set(key, result);
    result.then(() => {
      const timer = setTimeout(() => pending.delete(key), retentionMs);
      timer.unref?.();
    }, () => pending.delete(key));
    return result;
  };
}
