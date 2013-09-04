using AdvancedPhotoFilter.SelectionTools;
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
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AdvancedPhotoFilter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private const string CachedFileName = "img.jpg";
        private const string SharedFileName = "Share.jpg";

        #region Backing fields

        private FilterViewModel _selectedFilter;
        private BlendFunction _selectedBlendFunction;
        private bool _canApply;
        private WriteableBitmap _mainImage, _previewImage;
        private bool _isLoaded;
        private ISelectionTool _selectedTool;
        private byte _grayscale = 255, _grayscale2 = 255;
        private bool _hasImage;
        private bool _isWorking;

        #endregion

        #region Fields

        private Stream _originalStream;
        private EditingSession _editingSession;
        private int[] _buffer;
        private byte[] _maskBuffer;
        private int[] _oldPixels;
        private bool _filterApplied;
        private bool _noChanges = true;
        private WriteableBitmap _backup;
        private readonly IsolatedStorageFile _isoStore;
        private bool _queued;

        #endregion

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        #region Bindable properties

        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { Set(ref _isLoaded, value); }
        }

        public List<FilterViewModel> Filters { get; set; }
        public FilterViewModel SelectedFilter
        {
            get { return _selectedFilter; }
            set
            {
                if (!Set(ref _selectedFilter, value) || !CanApply)
                    return;

                ApplyExecute();
                NextFilterCommand.RaiseCanExecuteChanged();
                PreviousFilterCommand.RaiseCanExecuteChanged();
            }
        }

        public List<BlendFunction> BlendFunctions { get; set; }
        public BlendFunction SelectedBlendFunction
        {
            get { return _selectedBlendFunction; }
            set
            {
                if (!Set(ref _selectedBlendFunction, value) || !CanApply)
                    return;

                ApplyExecute();
                NextBlendFunctionCommand.RaiseCanExecuteChanged();
                PreviousBlendFunctionCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanApply
        {
            get { return _canApply; }
            set
            {
                Set(ref _canApply, value);
                ApplyCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasImage
        {
            get { return _hasImage; }
            set
            {
                Set(ref _hasImage, value);
                ShareCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public WriteableBitmap MainImage
        {
            get { return _mainImage; }
            set { Set(ref _mainImage, value); }
        }

        public WriteableBitmap PreviewImage
        {
            get { return _previewImage; }
            set { Set(ref _previewImage, value); }
        }

        public ISelectionTool SelectedTool
        {
            get { return _selectedTool; }
            set
            {
                if (Set(ref _selectedTool, value))
                    BindTool();
            }
        }

        public List<ISelectionTool> Tools { get; set; }

        public byte Grayscale
        {
            get { return _grayscale; }
            set
            {
                if (Set(ref _grayscale, value))
                    ApplyExecute();
            }
        }

        public byte Grayscale2
        {
            get { return _grayscale2; }
            set
            {
                if (Set(ref _grayscale2, value))
                    ApplyExecute();
            }
        }

        public bool IsWorking
        {
            get { return _isWorking; }
            set { Set(ref  _isWorking, value); }
        }

        #endregion

        #region Commands

        public RelayCommand ChooseCommand { get; set; }
        public RelayCommand ApplyCommand { get; set; }
        public RelayCommand UndoCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand ShareCommand { get; set; }

        public RelayCommand PreviousFilterCommand { get; set; }
        public RelayCommand NextFilterCommand { get; set; }

        public RelayCommand PreviousBlendFunctionCommand { get; set; }
        public RelayCommand NextBlendFunctionCommand { get; set; }

        #endregion

        public MainViewModel()
        {
            _isoStore = IsolatedStorageFile.GetUserStoreForApplication();

            Filters = new List<FilterViewModel>
            {
                FilterViewModel.Create("Antique", FilterFactory.CreateAntiqueFilter),
                FilterViewModel.Create("AutoEnhance", FilterFactory.CreateAutoEnhanceFilter, new AutoEnhanceConfiguration()),
                FilterViewModel.Create("Blur", FilterFactory.CreateBlurFilter, BlurLevel.Blur4),
                FilterViewModel.Create("Cartoon", FilterFactory.CreateCartoonFilter, true),
                FilterViewModel.Create("Emboss", FilterFactory.CreateEmbossFilter, .4f),
                FilterViewModel.Create("Fog", FilterFactory.CreateFogFilter),
                FilterViewModel.Create("MagicPen",FilterFactory.CreateMagicPenFilter),
                FilterViewModel.Create("Sepia",FilterFactory.CreateSepiaFilter),
                FilterViewModel.Create("Sketch",FilterFactory.CreateSketchFilter, SketchMode.Color),
                FilterViewModel.Create("Sketch-Gray",FilterFactory.CreateSketchFilter, SketchMode.Gray),
                FilterViewModel.Create("Solarize",FilterFactory.CreateSolarizeFilter, .3f),
            };
            SelectedFilter = Filters[3];
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

            Tools = new List<ISelectionTool>
                {
                    new BrushSelectionTool(),
                    new MagicWandTool(),
                    new RectSelectionTool()
                };
            SelectedTool = Tools[0];

            // register all commands here
            ChooseCommand = new RelayCommand(ChooseExecute);
            ApplyCommand = new RelayCommand(ApplyExecute, () => CanApply);
            UndoCommand = new RelayCommand(UndoExecute, () => _noChanges == false);
            SaveCommand = new RelayCommand(SaveExecute, () => HasImage);
            ShareCommand = new RelayCommand(ShareExecute, () => HasImage);

            PreviousFilterCommand = new RelayCommand(PreviousFilterExecute, () => Filters.First() != SelectedFilter);
            NextFilterCommand = new RelayCommand(NextFilterExecute, () => Filters.Last() != SelectedFilter);

            PreviousBlendFunctionCommand = new RelayCommand(PreviousBlendFunctionExecute, () => BlendFunctions.First() != SelectedBlendFunction);
            NextBlendFunctionCommand = new RelayCommand(NextBlendFunctionExecute, () => BlendFunctions.Last() != SelectedBlendFunction);
        }

        public async Task LoadAsync()
        {
            if (IsLoaded)
                return;

            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isoStore.FileExists(CachedFileName))
                {
                    using (var file = isoStore.OpenFile("img.jpg", FileMode.Open, FileAccess.Read))
                    {
                        if (file.Length != 0)
                        {
                            _originalStream = new MemoryStream((int)file.Length);
                            await file.CopyToAsync(_originalStream);
                            await ShowImageAsync();
                        }
                        else
                        {
                            CanApply = false;
                        }
                    }
                }
                else
                {
                    CanApply = false;
                }
            }

            IsLoaded = true;
        }

        private async Task ShowImageAsync()
        {
            _originalStream.Seek(0, SeekOrigin.Begin);
            Helpers.SafeDispose(ref _editingSession);
            _editingSession = await EditingSessionFactory.CreateEditingSessionAsync(_originalStream);
            ImageWidth = (int)_editingSession.Dimensions.Width;
            ImageHeight = (int)_editingSession.Dimensions.Height;

            MainImage = new WriteableBitmap(ImageWidth, ImageHeight);
            await _editingSession.RenderToWriteableBitmapAsync(MainImage);

            _buffer = new int[MainImage.PixelWidth * MainImage.PixelHeight];
            _maskBuffer = new byte[MainImage.PixelWidth * MainImage.PixelHeight];
            _oldPixels = new int[MainImage.PixelWidth * MainImage.PixelHeight];
            MainImage.Pixels.CopyTo(_oldPixels, 0);
            MainImage.Invalidate();
            PreviewImage = null;

            BindTool();

            if (_backup == null)
            {
                _backup = new WriteableBitmap(MainImage.PixelWidth, MainImage.PixelHeight);
                MainImage.Pixels.CopyTo(_backup.Pixels, 0);
            }

            CanApply = false;
            HasImage = true;
        }

        #region Tool manipulation

        public void ManipulationStarted(double imgWidth, double imgHeight, ManipulationStartedEventArgs e)
        {
            if (SelectedTool != null && !IsWorking)
            {
                SelectedTool.ImageWidth = imgWidth;
                SelectedTool.ImageHeight = imgHeight;
                SelectedTool.Started(e);
            }
        }

        public void ManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (SelectedTool != null && !IsWorking)
                SelectedTool.Delta(e);
        }

        public void ManipulationEnded(ManipulationCompletedEventArgs e)
        {
            if (SelectedTool != null && !IsWorking)
            {
                SelectedTool.Ended(e);
                CanApply = true;
                _noChanges = false;
                UndoCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Command implementations

        private void ChooseExecute()
        {
            var photoPicker = new PhotoChooserTask { ShowCamera = true };
            photoPicker.Completed += photoPicker_Completed;
            photoPicker.Show();
        }

        private async void photoPicker_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                _originalStream = e.ChosenPhoto;

                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                using (var file = isoStore.CreateFile("img.jpg"))
                {
                    e.ChosenPhoto.CopyTo(file);
                    file.Flush(true);
                }

                await ShowImageAsync();
            }
        }

        private async void ApplyExecute()
        {
            IsWorking = true;

            if (_editingSession == null || _noChanges)
                return;

            if (!CanApply)
            {
                _queued = true;
                return;
            }

            CanApply = false;

            _filterApplied = true;
            if (_filterApplied && _editingSession.CanUndo())
            {
                try { _editingSession.Undo(); }
                catch (Exception e)
                {
                    App.LogError(e);
                }
            }

            // apply filter to selected pixels only
            PreviewImage = new WriteableBitmap(ImageWidth, ImageHeight);
            _buffer.CopyTo(PreviewImage.Pixels, 0);

            using (var ms = PreviewImage.ToStream())
            {
                var editingSession = await EditingSessionFactory.CreateEditingSessionAsync(ms);
                editingSession.AddFilter(SelectedFilter.Creator());
                await editingSession.RenderToWriteableBitmapAsync(PreviewImage);
            }

            // remove unnecessary pixels
            //var removal = ClearTransparent ? Constants.White & 0xFFFFFF : Constants.White;
            // transparent white
            var removal = (0xFF << 24) |
                            (Grayscale << 16) |
                            (Grayscale << 8) |
                            (Grayscale);

            for (var i = 0; i < PreviewImage.Pixels.Length; ++i)
            {
                if (_maskBuffer[i] == 0)
                    PreviewImage.Pixels[i] = removal;

                if (_maskBuffer[i] != 0)
                    MainImage.Pixels[i] = ((Grayscale2 << 16) | (Grayscale2 << 8) | Grayscale2) | ((255 - _maskBuffer[i]) << 24);
                else
                    MainImage.Pixels[i] = _backup.Pixels[i];
            }

#if DEBUG
            try
            {
                using (var fileStream = _isoStore.CreateFile("MainImage.jpg"))
                    MainImage.SaveJpeg(fileStream, ImageWidth, ImageHeight, 0, 100);
                using (var fileStream = _isoStore.CreateFile("PreviewImage.jpg"))
                    PreviewImage.SaveJpeg(fileStream, ImageWidth, ImageHeight, 0, 100);
            }
            catch (Exception e)
            {
                App.LogError(e);
            }
#endif

            using (var ms = MainImage.ToStream())
            {
                Helpers.SafeDispose(ref _editingSession);
                _editingSession = await EditingSessionFactory.CreateEditingSessionAsync(ms);
            }

            using (var ms = PreviewImage.ToStream())
            using (var blendingSession = await EditingSessionFactory.CreateEditingSessionAsync(ms))
            {
                PreviewImage.Invalidate();

                _editingSession.AddFilter(FilterFactory.CreateBlendFilter(blendingSession, SelectedBlendFunction));
                var temp = new WriteableBitmap(MainImage.PixelWidth, MainImage.PixelHeight);
                await _editingSession.RenderToWriteableBitmapAsync(temp);
                MainImage = temp;
                if (SelectedTool != null)
                    SelectedTool.TargetImage = MainImage;

                UndoCommand.RaiseCanExecuteChanged();
                CanApply = true;
            }

            if (_queued)
            {
                _queued = false;
                ApplyExecute();
            }

            IsWorking = false;
        }

        private async void UndoExecute()
        {
            _buffer = new int[MainImage.PixelWidth * MainImage.PixelHeight];
            _maskBuffer = new byte[MainImage.PixelWidth * MainImage.PixelHeight];
            _filterApplied = false;

            _originalStream.Seek(0, SeekOrigin.Begin);
            _editingSession = await EditingSessionFactory.CreateEditingSessionAsync(_originalStream);
            await _editingSession.RenderToWriteableBitmapAsync(MainImage);
            MainImage.Invalidate();

            CanApply = false;
            _noChanges = true;
            UndoCommand.RaiseCanExecuteChanged();

            if (PreviewImage != null)
            {
                for (var i = 0; i < PreviewImage.Pixels.Length; ++i)
                    PreviewImage.Pixels[i] = 0;
                PreviewImage.Invalidate();
            }

            BindTool();
        }

        private void SaveExecute()
        {
            using (var mediaLibrary = new MediaLibrary())
            using (var stream = MainImage.ToStream())
            {
                mediaLibrary.SavePicture("SavedPicture.jpg", stream);
            }
        }

        private void ShareExecute()
        {
            using (var fileStream = _isoStore.CreateFile(SharedFileName))
            {
                MainImage.SaveJpeg(fileStream, ImageWidth, ImageHeight, 0, 100);

                var task = new ShareMediaTask { FilePath = SharedFileName };
                task.Show();
            }
        }

        private void PreviousFilterExecute()
        {
            if (Filters.First() != SelectedFilter)
                SelectedFilter = Filters[Filters.IndexOf(SelectedFilter) - 1];
        }

        private void NextFilterExecute()
        {
            if (Filters.Last() != SelectedFilter)
                SelectedFilter = Filters[Filters.IndexOf(SelectedFilter) + 1];
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

        #endregion

        private void BindTool()
        {
            if (SelectedTool == null)
            {
                return;
            }

            SelectedTool.TargetImage = MainImage;
            SelectedTool.Source = _oldPixels;
            SelectedTool.Target = _buffer;
            SelectedTool.MaskBuffer = _maskBuffer;
        }
    }
}
