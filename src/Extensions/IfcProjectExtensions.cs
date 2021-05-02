using GeometryGym.Ifc;
using System;
using System.Linq;

namespace ConvertIfc2Json
{
    /// <summary>
    /// Extends the <see cref="IfcProject"/> instances.
    /// </summary>
    public static class IfcProjectExtensions
    {
        /// <summary>
        /// Gets the scale to be applied on Json conversion of the ifc geometrical data.
        /// </summary>
        /// <param name="project">The geogym ifc project instance</param>
        /// <returns>The scale value</returns>
        public static double GetJsonConversionScale(this IfcProject project)
        {
            // Selection of the unit candidat element that could be used to compute the scale value
            var unitCandidat = project.UnitsInContext.Units
                                    .OfType<IfcSIUnit>()

                                    // REVIEW: Pas besoin de tout parcourir. par ailleurs first serait plus performant mais pourrait changer le comportement initial. A definir en fonction de l'objectif a atteindre ici
                                    .LastOrDefault(unit => unit.UnitType == IfcUnitEnum.LENGTHUNIT && unit.Name == IfcSIUnitName.METRE);

            // Creating the scale value from found candidat unit
            if (unitCandidat != null)
            {
                switch (unitCandidat.Prefix)
                {
                    case IfcSIPrefix.EXA: return Math.Pow(10, -18);
                    case IfcSIPrefix.PETA: return Math.Pow(10, -15);
                    case IfcSIPrefix.TERA: return Math.Pow(10, -12);
                    case IfcSIPrefix.GIGA: return Math.Pow(10, -9);
                    case IfcSIPrefix.MEGA: return Math.Pow(10, -6);
                    case IfcSIPrefix.KILO: return Math.Pow(10, -3);
                    case IfcSIPrefix.HECTO: return Math.Pow(10, -2);
                    case IfcSIPrefix.DECA: return 10;
                    case IfcSIPrefix.DECI: return Math.Pow(10, 1);
                    case IfcSIPrefix.CENTI: return Math.Pow(10, 2);
                    case IfcSIPrefix.MILLI: return Math.Pow(10, 3);
                    case IfcSIPrefix.MICRO: return Math.Pow(10, 6);
                    case IfcSIPrefix.NANO: return Math.Pow(10, 9);
                    case IfcSIPrefix.PICO: return Math.Pow(10, 12);
                    case IfcSIPrefix.FEMTO: return Math.Pow(10, 15);
                    case IfcSIPrefix.ATTO: return Math.Pow(10, 18);
                }
            }

            // No unit found or no profix mapped, returning default value
            return 1;
        }
    }
}
