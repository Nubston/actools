using System.Collections.Generic;
using System.Linq;
using AcManager.Tools.Helpers;
using AcManager.Tools.Objects;
using StringBasedFilter;

namespace AcManager.Tools.Filters.Testers {
    public class CarObjectTester : IParentTester<CarObject>, ITesterDescription {
        public static readonly CarObjectTester Instance = new CarObjectTester();

        internal static string InnerParameterFromKey(string key) {
            switch (key) {
                case "b":
                case "brand":
                case "newbrand":
                    return nameof(CarObject.Brand);

                case "class":
                    return nameof(CarObject.CarClass);

                case "parent":
                    return nameof(CarObject.Parent);

                case "dd":
                case "driven":
                case "drivendistance":
                    return nameof(CarObject.TotalDrivenDistance);

                case "topspeedachieved":
                case "tsa":
                    return nameof(CarObject.MaxSpeedAchieved);

                case "bhp":
                case "power":
                    return nameof(CarObject.SpecsBhp);

                case "torque":
                    return nameof(CarObject.SpecsTorque);

                case "weight":
                case "mass":
                    return nameof(CarObject.SpecsWeight);

                case "acceleration":
                    return nameof(CarObject.SpecsAcceleration);

                case "speed":
                case "topspeed":
                    return nameof(CarObject.SpecsTopSpeed);

                case "pw":
                case "pwratio":
                    return nameof(CarObject.SpecsPwRatio);

                case "maxrpm":
                    return nameof(CarObject.SpecsTorqueCurve);

                case "skin":
                case "skins":
                    return nameof(CarObject.SkinsEnabledWrappersList);
            }

            return null;
        }

        public static string InheritingParameterFromKey(string key) {
            return InnerParameterFromKey(key) ?? AcJsonObjectTester.InheritingParameterFromKey(key);
        }

        public string ParameterFromKey(string key) {
            return InheritingParameterFromKey(key);
        }

        private List<string> _list;

        public bool Test(CarObject obj, string key, ITestEntry value) {
            switch (key) {
                case "b":
                case "brand":
                    return value.Test(obj.Brand);

                case "newbrand":
                    if (_list == null) {
                        _list = FilesStorage.Instance.GetContentFiles(ContentCategory.BrandBadges).Select(x => x.Name).ToList();
                    }
                    return value.Test(!_list.Contains(obj.Brand));

                case "class":
                    return value.Test(obj.CarClass);

                case "parent":
                    return value.Test(obj.Parent?.DisplayName);

                case "dd":
                case "driven":
                case "drivendistance":
                    return value.Test(obj.TotalDrivenDistance);

                case "topspeedachieved":
                case "tsa":
                    return value.Test(obj.MaxSpeedAchieved);

                case "bhp":
                case "power":
                    return value.Test(obj.SpecsBhp);

                case "torque":
                    return value.Test(obj.SpecsTorque);

                case "weight":
                case "mass":
                    return value.Test(obj.SpecsWeight);

                case "acceleration":
                    return value.Test(obj.SpecsAcceleration);

                case "speed":
                case "topspeed":
                    return value.Test(obj.SpecsTopSpeed);

                case "maxrpm":
                    return value.Test(obj.GetRpmMaxValue());

                case "pw":
                case "pwratio":
                    return value.Test(obj.GetSpecsPwRatioValue());

                case "skins":
                    return value.Test(obj.SkinsEnabledWrappersList?.Count ?? 0);
            }

            return AcJsonObjectTester.Instance.Test(obj, key, value);
        }

        public bool TestChild(CarObject obj, string key, IFilter filter) {
            switch (key) {
                case null:
                case "skin":
                    var skins = obj.SkinsManager;
                    return skins.IsScanned && skins.LoadedOnly.Any(x => filter.Test(CarSkinObjectTester.Instance, x)); // TODO: non-scanned?

                case "parent":
                    return obj.Parent != null && filter.Test(Instance, obj.Parent);
            }

            return false;
        }

        public IEnumerable<KeywordDescription> GetDescriptions() {
            return new[] {
                new KeywordDescription("brand", "Car brand", KeywordType.String, KeywordPriority.Important, "b"),
                new KeywordDescription("newbrand", "With possibly invalid brand", KeywordType.Flag, KeywordPriority.Obscured),
                new KeywordDescription("class", "Car class", KeywordType.String, KeywordPriority.Important),
                new KeywordDescription("parent", "Main unmodified car", KeywordType.Child | KeywordType.String, KeywordPriority.Normal),
                new KeywordDescription("driven", "Driven distance", KeywordType.Number, KeywordPriority.Normal, "dd", "drivendistance"),
                new KeywordDescription("topspeedachieved", "Driven distance", KeywordType.Number, KeywordPriority.Normal, "tsa"),
                new KeywordDescription("power", "Power", KeywordType.Number, KeywordPriority.Normal, "bhp"),
                new KeywordDescription("torque", "Torque", KeywordType.Number, KeywordPriority.Normal),
                new KeywordDescription("weight", "Weight", KeywordType.Number, KeywordPriority.Normal, "mass"),
                new KeywordDescription("acceleration", "Acceleration", KeywordType.Number, KeywordPriority.Normal),
                new KeywordDescription("topspeed", "Top Speed", KeywordType.Number, KeywordPriority.Normal, "speed"),
                new KeywordDescription("maxrpm", "Max RPM from torque curve", KeywordType.Number, KeywordPriority.Normal),
                new KeywordDescription("pwratio", "P/W ratio", KeywordType.Number, KeywordPriority.Normal, "pw"),
                new KeywordDescription("skins", "Skins count", KeywordType.Number, KeywordPriority.Normal),
                new KeywordDescription("skin", "Skin", KeywordType.Child, KeywordPriority.Normal),
            }.Concat(AcJsonObjectTester.Instance.GetDescriptions());
        }
    }
}