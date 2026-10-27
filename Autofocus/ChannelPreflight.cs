using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using System.Linq;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>
    /// Comprueba que el canal pedido se puede medir y, si no, falla con un mensaje claro en lugar de medir otra cosa
    /// y anotar el canal como si se hubiera enfocado con él. Se usa antes de empezar y dentro del detector.
    /// </summary>
    public static class ChannelPreflight {

        public static string Describe(MeasurabilityProblem problem, string channel, int binX, int binY) {
            string name = FocusChannelState.DisplayName(channel);
            switch (problem) {
                case MeasurabilityProblem.NotColor: return Loc.T("AF.NotColor", name);
                case MeasurabilityProblem.Binned: return Loc.T("AF.Binned", name, binX, binY);
                case MeasurabilityProblem.InvalidChannel: return Loc.T("AF.BadChannel", channel ?? string.Empty);
                default: return string.Empty;
            }
        }

        /// <summary>Lanza ChannelNotMeasurableException si el canal no se puede medir con estos datos.</summary>
        public static void Ensure(string channel, bool isColor, int binX, int binY) {
            var problem = ChannelMeasurability.Check(channel, isColor, binX, binY);
            if (problem != MeasurabilityProblem.None) {
                throw new ChannelNotMeasurableException(problem, channel, Describe(problem, channel, binX, binY));
            }
        }

        /// <summary>
        /// Comprobación antes de empezar un autoenfoque, con lo que se sabe de la cámara y del perfil: el tipo de sensor y el binning
        /// que usará el autoenfoque (el del filtro actual si lo tiene, o el de las opciones del enfocador).
        /// </summary>
        public static MeasurabilityProblem Check(string channel, ICameraMediator camera, IFilterWheelMediator filterWheel, IProfileService profile, out int binX, out int binY) {
            ResolveAutofocusBinning(filterWheel, profile, out binX, out binY);

            bool isColor;
            var configured = profile.ActiveProfile.CameraSettings.BayerPattern;
            if (configured == BayerPatternEnum.None) isColor = false;
            else if (configured != BayerPatternEnum.Auto) isColor = true;
            else isColor = camera.GetInfo()?.SensorType != SensorType.Monochrome;

            return ChannelMeasurability.Check(channel, isColor, binX, binY);
        }

        /// <summary>Igual que Check, pero lanza la excepción con el mensaje listo para mostrar.</summary>
        public static void EnsureForAutofocus(string channel, ICameraMediator camera, IFilterWheelMediator filterWheel, IProfileService profile) {
            var problem = Check(channel, camera, filterWheel, profile, out int bx, out int by);
            if (problem != MeasurabilityProblem.None) {
                throw new ChannelNotMeasurableException(problem, channel, Describe(problem, channel, bx, by));
            }
        }

        private static void ResolveAutofocusBinning(IFilterWheelMediator filterWheel, IProfileService profile, out int binX, out int binY) {
            FilterInfo filter = null;
            var info = filterWheel?.GetInfo();
            if (info?.SelectedFilter != null) {
                filter = profile.ActiveProfile.FilterWheelSettings.FilterWheelFilters.FirstOrDefault(x => x.Position == info.SelectedFilter.Position);
            }
            if (filter?.AutoFocusBinning != null) {
                binX = filter.AutoFocusBinning.X;
                binY = filter.AutoFocusBinning.Y;
            } else {
                int b = profile.ActiveProfile.FocuserSettings.AutoFocusBinning;
                binX = b;
                binY = b;
            }
        }
    }
}
