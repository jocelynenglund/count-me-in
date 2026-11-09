// The views this page last loaded, for the ?debug pill.
const seen = new Map();
export const record = (name, value) => { seen.set(name, { type: name, at: new Date().toISOString(), value }); return value; };
export const loadedViews = () => [...seen.values()];
