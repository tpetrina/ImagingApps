using Coding4Fun.Toolkit.Controls;
using Common;
using GalaSoft.MvvmLight.Command;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using Nokia.Graphics.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PhotoBlender.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private const string FirstFileName = "first.jpg";
        private const string SecondFileName = "second.jpg";
        private const string SharedFileName = "SharedImageFromBlender.jpg";

        private WriteableBitmap _blendedImage;
        private WriteableBitmap _firstImage;
        private WriteableBitmap _secondImage;
        private BlendFunction _selectedBlendFunction;
        private bool _queuedApply;
        private bool _filterApplied;
        private bool _hasImages;
        private bool _inProgress;
        private bool _canChoose = true;

        public WriteableBitmap FirstImage
        {
            get { return _firstImage; }
            set { Set(ref _firstImage, value); }
        }

        public WriteableBitmap SecondImage
        {
            get { return _secondImage; }
            set { Set(ref _secondImage, value); }
        }

        public WriteableBitmap BlendedImage
        {
            get { return _blendedImage; }
            set { Set(ref _blendedImage, value); }
        }

        public RelayCommand ChooseCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand ShareCommand { get; set; }
        public RelayCommand SwapCommand { get; set; }

        public RelayCommand PreviousBlendFunctionCommand { get; set; }
        public RelayCommand NextBlendFunctionCommand { get; set; }

        public bool HasImages
        {
            get { return _hasImages; }
            set
            {
                if (Set(ref _hasImages, value))
                {
                    SwapCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanChoose
        {
            get { return _canChoose; }
            set
            {
                if (Set(ref _canChoose, value))
                {
                    ChooseCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool FilterApplied
        {
            get { return _filterApplied; }
            set
            {
                if (Set(ref _filterApplied, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                    ShareCommand.RaiseCanExecuteChanged();
                    SwapCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public List<BlendFunction> BlendFunctions { get; set; }
        public BlendFunction SelectedBlendFunction
        {
            get { return _selectedBlendFunction; }
            set
            {
                if (!Set(ref _selectedBlendFunction, value))
                    return;

                ApplyExecute();
                NextBlendFunctionCommand.RaiseCanExecuteChanged();
                PreviousBlendFunctionCommand.RaiseCanExecuteChanged();
            }
        }

        public MainViewModel()
        {
            BlendFunctions = new List<BlendFunction>
                {
                    BlendFunction.Multiply,
                    BlendFunction.Add,
                    BlendFunction.Color,
                    BlendFunction.Colorburn,
                    BlendFunction.Colordodge,
                    BlendFunction.Overlay,
                    BlendFunction.Softlight,
                    BlendFunction.Screen,
                    BlendFunction.Hardlight,
                    BlendFunction.Darken,
                    BlendFunction.Lighten,
                    BlendFunction.Hue,
                    BlendFunction.Exclusion,
                    BlendFunction.Difference,
                    BlendFunction.Pluslighten,
                };
            SelectedBlendFunction = BlendFunction.Multiply;

            ChooseCommand = new RelayCommand(ChooseExecute, () => CanChoose);
            SaveCommand = new RelayCommand(SaveExecute, () => FilterApplied);
            ShareCommand = new RelayCommand(ShareExecute, () => FilterApplied);
            SwapCommand = new RelayCommand(SwapExecute, () => FilterApplied);

            PreviousBlendFunctionCommand = new RelayCommand(PreviousBlendFunctionExecute, () => BlendFunctions.First() != SelectedBlendFunction);
            NextBlendFunctionCommand = new RelayCommand(NextBlendFunctionExecute, () => BlendFunctions.Last() != SelectedBlendFunction);
        }

        private void PreviousBlendFunctionExecute()
        {
            if (BlendFunctions.First() != SelectedBlendFunction)
                SelectedBlendFunction = BlendFunctions[BlendFunctions.IndexOf(SelectedBlendFunction) - 1];
        }

        private void NextBlendFunctionExecute()
        {
            if (BlendFunctions.Last() != SelectedBlendFunction)
                SelectedBlendFunction = BlendFunctions[BlendFunctions.IndexOf(SelectedBlendFunction) + 1];
        }

        public async Task LoadAsync()
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isoStore.FileExists(FirstFileName) &&
                    isoStore.FileExists(SecondFileName))
                {
                    using (var fileStream = isoStore.OpenFile(FirstFileName, FileMode.Open, FileAccess.Read))
                    {
                        var ms = new MemoryStream();
                        await fileStream.CopyToAsync(ms);
                        var bi = new BitmapImage();
                        bi.SetSource(ms);
                        FirstImage = new WriteableBitmap(bi);
                    }

                    using (var fileStream = isoStore.OpenFile(SecondFileName, FileMode.Open, FileAccess.Read))
                    {
                        var ms = new MemoryStream();
                        await fileStream.CopyToAsync(ms);
                        var bi = new BitmapImage();
                        bi.SetSource(ms);
                        SecondImage = new WriteableBitmap(bi);
                    }

                    ApplyExecute();
                }
            }
        }

        private async void ChooseExecute()
        {
            CanChoose = false;
            try
            {
                var chooser = new PhotoChooserTask { ShowCamera = true };
                var first = await chooser.ShowAsync();
                if (first.ChosenPhoto == null)
                {
                    var prompt = new ToastPrompt { Message = "Gotta choose something man..." };
                    prompt.Show();
                    return;
                }

                var second = await chooser.ShowAsync();
                if (second.ChosenPhoto == null)
                {
                    var prompt = new ToastPrompt { Message = "Also choose second one..." };
                    prompt.Show();
                    return;
                }

                var bi1 = new BitmapImage();
                bi1.SetSource(first.ChosenPhoto);

                var bi2 = new BitmapImage();
                bi2.SetSource(second.ChosenPhoto);

                WriteableBitmap wb1;
                WriteableBitmap wb2;
                if (bi2.PixelHeight != bi1.PixelHeight || bi2.PixelWidth != bi1.PixelWidth)
                {
                    // sorry for bad croping
                    var toast = new ToastPrompt { Message = "images are not of the same size, lame cropping incoming :/" };
                    toast.Show();

                    EditingSession session1 = null, session2 = null;
                    using (var stream1 = bi1.ToStream())
                    using (var stream2 = bi2.ToStream())
                        try
                        {
                            var task1 = EditingSessionFactory.CreateEditingSessionAsync(stream1);
                            var task2 = EditingSessionFactory.CreateEditingSessionAsync(stream2);
                            var sessions = await Task.WhenAll(task1, task2);
                            session1 = task1.Result;
                            session2 = task2.Result;

                            var cropRect = new Windows.Foundation.Rect(0, 0,
                                                                       Math.Min(bi1.PixelWidth, bi2.PixelWidth),
                                                                       Math.Min(bi1.PixelHeight, bi2.PixelHeight));

                            sessions[0].AddFilter(FilterFactory.CreateCropFilter(cropRect));
                            sessions[1].AddFilter(FilterFactory.CreateCropFilter(cropRect));
                            wb1 = new WriteableBitmap((int)cropRect.Width, (int)cropRect.Height);
                            wb2 = new WriteableBitmap((int)cropRect.Width, (int)cropRect.Height);

                            await Task.WhenAll(
                                sessions[0].RenderToWriteableBitmapAsync(wb1),
                                sessions[1].RenderToWriteableBitmapAsync(wb2)
                                );

                        }
                        finally
                        {
                            Helpers.SafeDispose(ref session1);
                            Helpers.SafeDispose(ref session2);
                        }
                }
                else
                {
                    wb1 = new WriteableBitmap(bi1);
                    wb2 = new WriteableBitmap(bi2);
                }

                // save images
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                using (IsolatedStorageFileStream fileStream1 = isoStore.CreateFile(FirstFileName),
                                                 fileStream2 = isoStore.CreateFile(SecondFileName))
                {
                    wb1.SaveJpeg(fileStream1, wb1.PixelWidth, wb1.PixelHeight, 0, 100);
                    wb2.SaveJpeg(fileStream2, wb2.PixelWidth, wb2.PixelHeight, 0, 100);
                }

                FirstImage = wb1;
                SecondImage = wb2;

                ApplyExecute();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                CanChoose = true;
            }
        }

        private async void ApplyExecute()
        {
            if (FirstImage == null || SecondImage == null)
                return;

            if (_inProgress)
            {
                _queuedApply = true;
                return;
            }

            FilterApplied = false;
            _inProgress = true;

            using (var firstStream = FirstImage.ToStream())
            using (var secondStream = SecondImage.ToStream())
            using (var session = await EditingSessionFactory.CreateEditingSessionAsync(firstStream))
            using (var blendingSession = await EditingSessionFactory.CreateEditingSessionAsync(secondStream))
            {
                session.AddFilter(FilterFactory.CreateBlendFilter(blendingSession, SelectedBlendFunction));
                var wb = new WriteableBitmap(FirstImage.PixelWidth, FirstImage.PixelHeight);
                await session.RenderToWriteableBitmapAsync(wb);
                BlendedImage = wb;
            }

            _inProgress = false;
            FilterApplied = true;
            if (_queuedApply)
            {
                _queuedApply = false;
                ApplyExecute();
            }
        }

        private void SwapExecute()
        {
            var temp = SecondImage;
            SecondImage = FirstImage;
            FirstImage = temp;
            ApplyExecute();
        }

        private void ShareExecute()
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            using (var fileStream = isoStore.CreateFile(SharedFileName))
            {
                BlendedImage.SaveJpeg(fileStream, BlendedImage.PixelWidth, BlendedImage.PixelHeight, 0, 100);

                var task = new ShareMediaTask { FilePath = SharedFileName };
                task.Show();
            }
        }

        private void SaveExecute()
        {
            using (var mediaLibrary = new MediaLibrary())
            {
                mediaLibrary.SavePicture("SavedPicture.jpg", BlendedImage.ToStream());
            }
        }
    }
}
