using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QualTrack.UI.Models
{
    /// <summary>
    /// Model for a crew served weapon qualification entry (3591/2 form)
    /// </summary>
    public class CrewServedWeaponEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _gunnerName = string.Empty;
        private string _gunnerDodId = string.Empty;
        private string _gunnerRankRate = string.Empty;
        private string _assistantGunnerName = string.Empty;
        private string _assistantGunnerDodId = string.Empty;
        private string _assistantGunnerRankRate = string.Empty;
        private string _ammunitionHandlerName = string.Empty;
        private string _ammunitionHandlerDodId = string.Empty;
        private string _ammunitionHandlerRankRate = string.Empty;
        private int? _cofScore;
        private int? _lightFreeP1;
        private int? _lightFreeP2;
        private int? _lightFreeP3;
        private int? _lightFreeP4;
        private int? _lightFreeP5;
        private int? _lightFreeP6;
        private int? _lightTeP1;
        private int? _lightTeP2;
        private int? _lightTeP3;
        private int? _lightTeP4;
        private int? _lightTeP5;
        private int? _heavyFreeP1;
        private int? _heavyFreeP2;
        private int? _heavyFreeP3;
        private int? _heavyFreeP4;
        private int? _heavyFreeP5;
        private int? _heavyFreeP6;
        private int? _heavyTeP1;
        private int? _heavyTeP2;
        private int? _heavyTeP3;
        private int? _heavyTeP4;
        private int? _heavyTeP5;

        public string GunnerName
        {
            get => _gunnerName;
            set => SetProperty(ref _gunnerName, value);
        }

        public string GunnerDodId
        {
            get => _gunnerDodId;
            set => SetProperty(ref _gunnerDodId, value);
        }

        public string GunnerRankRate
        {
            get => _gunnerRankRate;
            set => SetProperty(ref _gunnerRankRate, value);
        }

        public string AssistantGunnerName
        {
            get => _assistantGunnerName;
            set => SetProperty(ref _assistantGunnerName, value);
        }

        public string AssistantGunnerDodId
        {
            get => _assistantGunnerDodId;
            set => SetProperty(ref _assistantGunnerDodId, value);
        }

        public string AssistantGunnerRankRate
        {
            get => _assistantGunnerRankRate;
            set => SetProperty(ref _assistantGunnerRankRate, value);
        }

        public string AmmunitionHandlerName
        {
            get => _ammunitionHandlerName;
            set => SetProperty(ref _ammunitionHandlerName, value);
        }

        public string AmmunitionHandlerDodId
        {
            get => _ammunitionHandlerDodId;
            set => SetProperty(ref _ammunitionHandlerDodId, value);
        }

        public string AmmunitionHandlerRankRate
        {
            get => _ammunitionHandlerRankRate;
            set => SetProperty(ref _ammunitionHandlerRankRate, value);
        }

        public int? COFScore
        {
            get => _cofScore;
            set => SetProperty(ref _cofScore, value);
        }

        public int? LightFreeP1
        {
            get => _lightFreeP1;
            set => SetProperty(ref _lightFreeP1, value);
        }

        public int? LightFreeP2
        {
            get => _lightFreeP2;
            set => SetProperty(ref _lightFreeP2, value);
        }

        public int? LightFreeP3
        {
            get => _lightFreeP3;
            set => SetProperty(ref _lightFreeP3, value);
        }

        public int? LightFreeP4
        {
            get => _lightFreeP4;
            set => SetProperty(ref _lightFreeP4, value);
        }

        public int? LightFreeP5
        {
            get => _lightFreeP5;
            set => SetProperty(ref _lightFreeP5, value);
        }

        public int? LightFreeP6
        {
            get => _lightFreeP6;
            set => SetProperty(ref _lightFreeP6, value);
        }

        public int? LightTeP1
        {
            get => _lightTeP1;
            set => SetProperty(ref _lightTeP1, value);
        }

        public int? LightTeP2
        {
            get => _lightTeP2;
            set => SetProperty(ref _lightTeP2, value);
        }

        public int? LightTeP3
        {
            get => _lightTeP3;
            set => SetProperty(ref _lightTeP3, value);
        }

        public int? LightTeP4
        {
            get => _lightTeP4;
            set => SetProperty(ref _lightTeP4, value);
        }

        public int? LightTeP5
        {
            get => _lightTeP5;
            set => SetProperty(ref _lightTeP5, value);
        }

        public int? HeavyFreeP1
        {
            get => _heavyFreeP1;
            set => SetProperty(ref _heavyFreeP1, value);
        }

        public int? HeavyFreeP2
        {
            get => _heavyFreeP2;
            set => SetProperty(ref _heavyFreeP2, value);
        }

        public int? HeavyFreeP3
        {
            get => _heavyFreeP3;
            set => SetProperty(ref _heavyFreeP3, value);
        }

        public int? HeavyFreeP4
        {
            get => _heavyFreeP4;
            set => SetProperty(ref _heavyFreeP4, value);
        }

        public int? HeavyFreeP5
        {
            get => _heavyFreeP5;
            set => SetProperty(ref _heavyFreeP5, value);
        }

        public int? HeavyFreeP6
        {
            get => _heavyFreeP6;
            set => SetProperty(ref _heavyFreeP6, value);
        }

        public int? HeavyTeP1
        {
            get => _heavyTeP1;
            set => SetProperty(ref _heavyTeP1, value);
        }

        public int? HeavyTeP2
        {
            get => _heavyTeP2;
            set => SetProperty(ref _heavyTeP2, value);
        }

        public int? HeavyTeP3
        {
            get => _heavyTeP3;
            set => SetProperty(ref _heavyTeP3, value);
        }

        public int? HeavyTeP4
        {
            get => _heavyTeP4;
            set => SetProperty(ref _heavyTeP4, value);
        }

        public int? HeavyTeP5
        {
            get => _heavyTeP5;
            set => SetProperty(ref _heavyTeP5, value);
        }

        public bool IsQualified => COFScore.HasValue && COFScore.Value >= 100;
    }
}
