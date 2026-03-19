using CCD.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCD.Strategy
{
    public static class StrategyFactory
    {
        private static readonly Dictionary<ShapeEnum, IShapeStrategy> strategyCache = new Dictionary<ShapeEnum, IShapeStrategy>();

        public static IShapeStrategy CreateStrategy(ShapeEnum shapeEnum)
        {
            if (strategyCache.ContainsKey(shapeEnum))
            {
                return strategyCache[shapeEnum];
            }

            string className = "CCD.Strategy." + shapeEnum.ToString() + "Strategy";
            Type strategyType = Type.GetType(className);

            if (strategyType != null)
            {
                if (Activator.CreateInstance(strategyType) is IShapeStrategy strategy)
                {
                    strategyCache.Add(shapeEnum, strategy);
                    return strategy;
                }
            }
            return null;
        }
    }
}
