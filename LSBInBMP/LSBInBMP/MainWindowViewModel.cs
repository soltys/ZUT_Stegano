using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ImageHelperLibrary;
using LSBInBMP.View.ViewModel;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace LSBInBMP
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private ICommand _openFileCommand;
        private BitmapImage _sourceImage;
        private BitmapImage _cryptedImageSource = new BitmapImage();
        private RelayCommand _insertMessage;
        private RelayCommand _readMessage;
        private RelayCommand _saveFile;

        public MainWindowViewModel()
        {
            _openFileCommand = new RelayCommand(OpenFileAction);
            _sourceImage = new BitmapImage(new Uri("/source.bmp", UriKind.Relative));
            
            _message =
                @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam tristique commodo neque ac venenatis. Donec tincidunt orci at turpis vulputate euismod. Vestibulum tempor interdum massa, non tincidunt nisl egestas ac. Integer iaculis, leo vitae laoreet porttitor, metus urna maximus ipsum, a luctus ligula dolor eu lectus. Fusce a libero eleifend, volutpat mi ut, scelerisque quam. Nulla facilisi. Donec et egestas felis. Aenean sollicitudin lorem non mi laoreet rhoncus.";

            _insertMessage = new RelayCommand(InsertMessageAction);
            _readMessage = new RelayCommand(ReadMessageAction);
            _saveFile = new RelayCommand(SaveFileAction);

        }

        private void SaveFileAction(object obj)
        {
            MemoryStream ms = new MemoryStream();
            BitmapEncoder encode = new BmpBitmapEncoder();
            encode.Frames.Add(BitmapFrame.Create(_cryptedImageSource));
            encode.Save(ms);
            var bmpData = ms.ToArray();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "BMP Files (*.bmp)|*.bmp";
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, bmpData);
            }

        }


        private void ReadMessageAction(object obj)
        {
            MemoryStream ms = new MemoryStream();
            BitmapEncoder encode = new BmpBitmapEncoder();

            encode.Frames.Add(BitmapFrame.Create(_sourceImage));
            encode.Save(ms);
            var bmpData = ms.ToArray();
            BitmapManipulator manipulator = new BitmapManipulator(bmpData);
            var messageRead = manipulator.ReadMessage();

            MessageBox.Show(messageRead, "Odczytana wiadomość");
        }

        private static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        private void InsertMessageAction(object parameter)
        {
            string message = parameter as string;

            MemoryStream ms = new MemoryStream();
            BitmapEncoder encode = new BmpBitmapEncoder();
            
            encode.Frames.Add(BitmapFrame.Create(_sourceImage));            
            encode.Save(ms);
            var bmpData = ms.ToArray();
            BitmapManipulator manipulator = new BitmapManipulator(bmpData);
            manipulator.InsertMessage(message);

            var imageSource = new BitmapImage();
            MemoryStream imageMS = new MemoryStream(manipulator.Bytes);

            imageSource.BeginInit();
            imageSource.StreamSource = imageMS;
            imageSource.EndInit();

            CryptedImage = imageSource;
            if (bmpData.Length != manipulator.Bytes.Length)
            {
                Debugger.Break();
            }

        }

        public ICommand OpenFile
        {
            get { return _openFileCommand; }
        }

        public BitmapImage SourceImage
        {
            get { return _sourceImage; }
            set
            {
                _sourceImage = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage CryptedImage
        {
            get { return _cryptedImageSource; }
            set
            {
                _cryptedImageSource = value;
                OnPropertyChanged();
            }
        }

        public ICommand InsertMessage
        {
            get { return _insertMessage; }
        }

        public ICommand ReadMessage
        {
            get { return _readMessage; }
        }

        public ICommand SaveFile
        {
            get { return _saveFile; }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(); }

        }


        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(); }

        }

        private string _stegano;
        public string Stegano
        {
            get { return _stegano; }
            set { _stegano = value; OnPropertyChanged(); }

        }

        public void OpenFileAction(object sender)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".bmp";
            dlg.Filter = "BMP Files (*.bmp)|*.bmp";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                SourceImage = new BitmapImage(new Uri(filename));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
