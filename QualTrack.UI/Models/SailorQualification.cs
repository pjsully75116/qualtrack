using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QualTrack.UI
{
    public class SailorQualification : INotifyPropertyChanged
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
        private string _fullName = string.Empty;
        private string _dodId = string.Empty;
        private string _rankRate = string.Empty;

        public string FullName 
        { 
            get => _fullName; 
            set => SetProperty(ref _fullName, value); 
        }
        
        public string DodId 
        { 
            get => _dodId; 
            set => SetProperty(ref _dodId, value); 
        }
        
        public string RankRate 
        { 
            get => _rankRate; 
            set => SetProperty(ref _rankRate, value); 
        }
        public string DutySection { get; set; } = string.Empty;
        public string Designator { get; set; } = string.Empty;
        public string AdminStatus { get; set; } = string.Empty;
        public int? NHQC { get; set; }
        public int? HQC { get; set; }
        public int? RQC { get; set; }
        public int? SPWCT1 { get; set; }
        public int? SPWCT2 { get; set; }
        public int? SPWCT3 { get; set; }
        public int? SPWC { get; set; }
        public string SPWCQualified { get; set; } = string.Empty; // "Q" or "U"
        public int? HPWCT1 { get; set; }
        public int? HPWCT2 { get; set; }
        public int? HPWCT3 { get; set; }
        public string HPWCQualified { get; set; } = string.Empty; // "Q" or "U"
        public int? HLLC { get; set; }
        public int? RLLC { get; set; }
        public int? M240Score { get; set; }
        public int? OtherScore { get; set; }
    }
} 