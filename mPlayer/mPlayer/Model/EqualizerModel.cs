using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace mPlayer.Model
{
    public class EqualizerModel : BindableObject
    {
        #region Constants

        public static int[] EQ_FREQ = new int[] { 32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        private const string PRESETS_DIR = "Presets";
        private const string PRESETS_EXT = ".eq";

        #endregion

        #region Class

        public class EqualizerItem : BindableObject
        {
            private int type;
            public int Type
            {
                get { return type; }
                set
                {
                    type = value;
                    OnPropertyChanged("type");
                }
            }

            private string name;
            public string Name
            {
                get { return name; }
                set
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }

            private string values;
            public string Values
            {
                get { return values; }
                set
                {
                    values = value;
                    OnPropertyChanged("Values");
                }
            }
        }

        #endregion

        #region Properties

        private ObservableCollection<EqualizerItem> presets;
        public ObservableCollection<EqualizerItem> Presets
        {
            get { return presets; }
            set
            {
                presets = value;
                OnPropertyChanged("Presets");
            }
        }

        #endregion

        public EqualizerModel()
        {
            LoadPresets();
        }

        private void LoadPresets()
        {
            Presets = new ObservableCollection<EqualizerItem>()
            {
                new EqualizerItem() { Name = "Off", Type = 0 },
                new EqualizerItem() { Name = "Custom", Type = 1 },
                new EqualizerItem() { Name = "Bass", Type = 2, Values = "9;6;2;0;-0,7;-1;-1;0,5;3;6" },
                new EqualizerItem() { Name = "Flat", Type = 2, Values = "-5;-5;-2;0;2;3;3;3;3;3" },
                new EqualizerItem() { Name = "Normal", Type = 2, Values = "0;0;0;0;0;0;0;0;0;0" },
                new EqualizerItem() { Name = "Pop", Type = 2, Values = "-1;1;3;4;4;3;2;1;2;4" },
                new EqualizerItem() { Name = "Rock", Type = 2, Values = "5;3;0;0;0;3;4;4;3,3;3" }
            };

            foreach (var file in Directory.GetFiles(GetPresetsFolder, "*" + PRESETS_EXT))
            {
                var item = new EqualizerItem()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Type = 2
                };

                var text = File.ReadAllText(file);
                var values = text.Split(';');

                if (values.Length == 10)
                {
                    item.Values = text;
                    Presets.Add(item);
                }
            }
        }

        private string GetPresetsFolder
        {
            get
            {
                var dir = Path.Combine(MainModel.GetUserDataFolder, PRESETS_DIR);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public EqualizerItem SavePreset(string name, float[] values)
        {
            var newName = name;
            var path = Path.Combine(GetPresetsFolder, name + PRESETS_EXT);

            int index = 1;
            while (File.Exists(path))
            {
                newName = name + " (" + index + ")";
                path = Path.Combine(GetPresetsFolder, newName + PRESETS_EXT);
                index++;
            }

            var value = GetValueString(values);

            File.WriteAllText(path, value);

            var item = new EqualizerItem() { Name = newName, Type = 2, Values = value };
            Presets.Add(item);

            return item;
        }

        private string GetValueString(float[] values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i]);
                if (i < values.Length - 1) sb.Append(";");
            }

            return sb.ToString();
        }
    }
}
