using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Utils
{
    /// <summary>
    /// Utility for caching values that are expensive to calculate.
    /// </summary>
    public class CachedValue<T>
    {
        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        private int lastCacheTime = -1;

        private T cachedValue;
        public T Value
        {
            get
            {
                TryCacheValue(false);
                
                return cachedValue;
            }
            set => CacheValueInternal(value);
        }

        public delegate T ValueCalculator();
        private ValueCalculator calculator;
        
        public delegate bool CacheInvalidator();
        
        /// <summary>
        /// If specified, a Cache Invalidator checks whether the cache is out of date and needs to be rebuilt.
        /// So it should return true if the cache is out of date and ought to be rebuilt, false if the currently
        /// cached value should continue being used.
        /// </summary>
        private CacheInvalidator cacheInvalidator;
        private bool hasCacheInvalidator;
        
        public delegate void CacheUpdateHandler();
        private CacheUpdateHandler cacheUpdateHandler;

        public CachedValue(ValueCalculator calculator)
        {
            this.calculator = calculator;
        }

        public CachedValue(
            ValueCalculator calculator, CacheInvalidator cacheInvalidator,
            CacheUpdateHandler cacheUpdateHandler = null) : this(calculator)
        {
            this.cacheInvalidator = cacheInvalidator;
            hasCacheInvalidator = true;
            this.cacheUpdateHandler = cacheUpdateHandler;
        }

        public void Cleanup()
        {
            cacheInvalidator = null;
            cacheUpdateHandler = null;
        }

        private bool ShouldReCacheValue(bool ignoreEnableState)
        {
            // Never cache automatically if we are explicitly disabled.
            if (!ignoreEnableState && !Enabled)
                return false;

            // If a cache invalidator is specified and the cache has not been invalidated, do not recache.
            if (hasCacheInvalidator && !cacheInvalidator())
                return false;

            // Never cache more than once per frame.
            return Time.renderedFrameCount > lastCacheTime;
        }

        /// <summary>
        /// Try to update the cached value but skip if caching has been disabled.
        /// </summary>
        /// <returns>Whether it updated the cached value or not.</returns>
        public bool TryCacheValue(bool ignoreEnableState)
        {
            if (!ShouldReCacheValue(ignoreEnableState))
                return false;
            
            CacheValueInternal(calculator());
            return true;
        }
        
        private void CacheValueInternal(T value)
        {
            cachedValue = value;
            UpdateCacheTime();
            cacheUpdateHandler?.Invoke();
        }

        private void UpdateCacheTime()
        {
            lastCacheTime = Time.renderedFrameCount;
        }

        public T ForceCacheValue()
        {
            T value = calculator();
            CacheValueInternal(value);
            return value;
        }

        public static implicit operator T(CachedValue<T> cachedValue)
        {
            return cachedValue.Value;
        }
    }
}
