// React-compatible callback identity for controlled component fixtures.
// Reset the cursor on each render, not the memoized callbacks.
module.exports = function callbackHookFixture() {
  let index = 0;
  const callbacks = [];
  return {
    reset() { index = 0; },
    useCallback(fn, deps) {
      const key = index++, old = callbacks[key];
      if (!old || !deps || !old.deps || deps.length !== old.deps.length ||
          deps.some((value, i) => !Object.is(value, old.deps[i]))) {
        callbacks[key] = { fn, deps };
      }
      return callbacks[key].fn;
    },
  };
};
