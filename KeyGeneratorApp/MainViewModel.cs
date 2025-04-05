using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace KeyGeneratorApp
{
    internal class MainViewModel : ObservableObject
    {
        private string _pin = "000000";
        private string _outputPath = "./";
        private string _privateKeyFileName = "privateKey";
        private string _publicKeyFileName = "publicKey";

        public string Pin
        {
            get => _pin;
            set
            {
                if (_pin != value)
                {
                    _pin = value;
                    OnPropertyChanged(); //Check if we need to pass the property name
                }
            }
        }

        public string OutputDirectory
        {
            get => _outputPath;
            set
            {
                if (_outputPath != value)
                {
                    _outputPath = value;
                    OnPropertyChanged(); //Check if we need to pass the property name
                }
            }
        }

        public string PrivateKeyFileName
        {
            get => _privateKeyFileName;
            set
            {
                if (_privateKeyFileName != value)
                {
                    _privateKeyFileName = value;
                    OnPropertyChanged(); //Check if we need to pass the property name
                }
            }
        }

        public string PublicKeyFileName
        {
            get => _publicKeyFileName;
            set
            {
                if (_publicKeyFileName != value)
                {
                    _publicKeyFileName = value;
                    OnPropertyChanged(); //Check if we need to pass the property name
                }
            }
        }

        public MainViewModel()
        {

        }
    }
}