using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FLP;
using Kermalis.MIDI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MIDIProgramSplitter.GUI;

internal sealed partial class MainWindow : Window
{
	private readonly FilePickerFileType _midiPickerType;
	private readonly FilePickerFileType _flpPickerType;
	private readonly FilePickerFileType _dlsPickerType;

	private readonly FLPSaveOptions _flpOptions;

	private Splitter? _splitter;
	private string? _fileName;

	public MainWindow()
	{
		InitializeComponent();

		_flpOptions = new FLPSaveOptions();
		UpdateControlFLPOptions();

		_midiPickerType = new FilePickerFileType("MIDI Files")
		{
			Patterns = new string[]
			{
				"*.mid",
				"*.midi",
			},
		};
		_flpPickerType = new FilePickerFileType("FLP Files")
		{
			Patterns = new string[]
			{
				"*.flp",
			},
		};
		_dlsPickerType = new FilePickerFileType("DLS Files")
		{
			Patterns = new string[]
			{
				"*.dls",
			},
		};
	}

	public void HandleArgs(string[] args)
	{

	}

	private async void OnClickedBrowseInputMIDI(object? sender, RoutedEventArgs e)
	{
		var options = new FilePickerOpenOptions
		{
			AllowMultiple = false,
			Title = "Select Input MIDI File",
			FileTypeFilter = new FilePickerFileType[]
			{
				_midiPickerType,
			},
		};

		IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(options);
		if (result.Count != 1)
		{
			return;
		}

		try
		{
			IStorageFile file = result[0];
			string filePath = GetFilePath(file);
			InputMIDI.Text = filePath;
			_fileName = Path.GetFileNameWithoutExtension(filePath);

			Stream fs = await file.OpenReadAsync();
			var midi = new MIDIFile(fs);
			await fs.DisposeAsync();

			_flpOptions.DefaultMIDIVolume = (byte)DefaultMIDIVolume.Value!;

			_splitter = new Splitter(midi, _flpOptions.DefaultMIDIVolume); // TODO: MIDI import options
			LogControl.Items = _splitter.SLog;
		}
		catch (Exception ex)
		{
			string str = ex.ToString();
			_splitter = null;
			InputMIDI.Text = string.Empty;
			LogControl.Items = new string[1] { str };
			await DisplayPopup("Error Opening MIDI", str);
		}
	}
	private async void OnClickedBrowseInputDLS(object? sender, RoutedEventArgs e)
	{
		var options = new FilePickerOpenOptions
		{
			AllowMultiple = false,
			Title = "Select Input DLS File",
			FileTypeFilter = new FilePickerFileType[]
			{
				_dlsPickerType,
			},
		};

		IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(options);
		if (result.Count != 1)
		{
			return;
		}

		InputDLS.Text = GetFilePath(result[0]);
	}

	private async void OnClickedSaveMIDI(object? sender, RoutedEventArgs e)
	{
		if (_splitter is null)
		{
			return;
		}

		var options = new FilePickerSaveOptions
		{
			DefaultExtension = ".mid",
			Title = "Select Output MIDI File",
			FileTypeChoices = new FilePickerFileType[]
			{
				_midiPickerType,
			},
			ShowOverwritePrompt = true,
			SuggestedFileName = _fileName! + ".mid",
		};

		IStorageFile? result = await StorageProvider.SaveFilePickerAsync(options);
		if (result is null)
		{
			return;
		}

		string outPath = GetFilePath(result);

		try
		{
			using (FileStream s = File.Create(outPath))
			{
				_splitter.SaveMIDI(s);
			}
		}
		catch (Exception ex)
		{
			string str = ex.ToString();
			await DisplayPopup("Error Saving MIDI", str);
		}

		await DisplayPopup("Success", string.Format("Successfully saved \"{0}\".", outPath));
	}
	private async void OnClickedSaveFLP(object? sender, RoutedEventArgs e)
	{
		if (_splitter is null)
		{
			return;
		}

		var options = new FilePickerSaveOptions
		{
			DefaultExtension = ".flp",
			Title = "Select Output FLP File",
			FileTypeChoices = new FilePickerFileType[]
			{
				_flpPickerType,
			},
			ShowOverwritePrompt = true,
			SuggestedFileName = _fileName! + ".flp",
		};

		IStorageFile? result = await StorageProvider.SaveFilePickerAsync(options);
		if (result is null)
		{
			return;
		}

		UpdateFLPOptions();
		string outPath = GetFilePath(result);

		try
		{
			using (FileStream s = File.Create(outPath))
			{
				_splitter.SaveFLP(s, _flpOptions);
			}
		}
		catch (Exception ex)
		{
			string str = ex.ToString();
			await DisplayPopup("Error Saving FLP", str);
			return;
		}

		await DisplayPopup("Success", string.Format("Successfully saved \"{0}\".", outPath));
	}

	private static string GetFilePath(IStorageFile f)
	{
		return f.Path.LocalPath;
	}

	private void UpdateControlFLPOptions()
	{
		FLPVersionCompat.SelectedIndex = (int)_flpOptions.FLVersionCompat;

		// DLSPath

		FLPPitchBendRange.Value = _flpOptions.PitchBendRange;
		DefaultMIDIVolume.Value = _flpOptions.DefaultMIDIVolume;

		FLPAutomationTrackSize.Value = (decimal)_flpOptions.AutomationTrackSize * 100;
		FLPAutomationGrouping.SelectedIndex = (int)_flpOptions.AutomationGrouping;
		FLPCollapseAutomationGroups.IsChecked = _flpOptions.CollapseAutomationGroups;

		FLPAppendInstrumentNamesToPatterns.IsChecked = _flpOptions.AppendInstrumentNamesToPatterns;

		FLPInstrumentTrackColoring.SelectedIndex = (int)_flpOptions.InstrumentTrackColoring;
		FLPAutomationTrackColoring.SelectedIndex = (int)_flpOptions.AutomationTrackColoring;
		FLPMIDIOutColoring.SelectedIndex = (int)_flpOptions.MIDIOutColoring;
		FLPPatternColoring.SelectedIndex = (int)_flpOptions.PatternColoring;
		FLPInsertColoring.SelectedIndex = (int)_flpOptions.InsertColoring;
		FLPAutomationColoring.SelectedIndex = (int)_flpOptions.AutomationColoring;
	}
	private void UpdateFLPOptions()
	{
		_flpOptions.FLVersionCompat = (FLVersionCompat)FLPVersionCompat.SelectedIndex;

		_flpOptions.DLSPath = InputDLS.Text ?? string.Empty;

		_flpOptions.PitchBendRange = (int)FLPPitchBendRange.Value!.Value;
		// DefaultMIDIVolume

		_flpOptions.AutomationTrackSize = (float)(FLPAutomationTrackSize.Value!.Value / 100);
		_flpOptions.AutomationGrouping = (FLPSaveOptions.AutomationGroupMode)FLPAutomationGrouping.SelectedIndex;
		_flpOptions.CollapseAutomationGroups = FLPCollapseAutomationGroups.IsChecked!.Value;

		_flpOptions.AppendInstrumentNamesToPatterns = FLPAppendInstrumentNamesToPatterns.IsChecked!.Value;

		_flpOptions.InstrumentTrackColoring = (FLPSaveOptions.InstrumentTrackColorMode)FLPInstrumentTrackColoring.SelectedIndex;
		_flpOptions.AutomationTrackColoring = (FLPSaveOptions.AutomationTrackColorMode)FLPAutomationTrackColoring.SelectedIndex;
		_flpOptions.MIDIOutColoring = (FLPSaveOptions.MIDIOutColorMode)FLPMIDIOutColoring.SelectedIndex;
		_flpOptions.PatternColoring = (FLPSaveOptions.PatternColorMode)FLPPatternColoring.SelectedIndex;
		_flpOptions.InsertColoring = (FLPSaveOptions.InsertColorMode)FLPInsertColoring.SelectedIndex;
		_flpOptions.AutomationColoring = (FLPSaveOptions.AutomationColorMode)FLPAutomationColoring.SelectedIndex;
	}

	private async Task DisplayPopup(string title, string text)
	{
		var okButton = new Button
		{
			[Grid.RowProperty] = 1,
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
			VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
			Content = "OK",
		};
		var backgroundWindow = new Window
		{
			Title = title,
			SizeToContent = SizeToContent.WidthAndHeight,
			MinWidth = 200,
			MaxWidth = 1400,
			MinHeight = 200,
			MaxHeight = 650,
			TransparencyLevelHint = WindowTransparencyLevel.AcrylicBlur,
			Background = Brushes.Transparent,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			ShowInTaskbar = true,
			Topmost = true,
			CanResize = false, // Thanks Avalonia for never allowing me to disable the maximize button while allowing manual resizing
			Content = new Grid
			{
				[AutomationProperties.AccessibilityViewProperty] = AccessibilityView.Content,
				Margin = new Thickness(5),
				RowDefinitions =
				{
					new RowDefinition { Height = new GridLength(2, GridUnitType.Star) },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				},
				Children =
				{
					new TextBlock
					{
						[Grid.RowProperty] = 0,
						HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
						VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
						TextWrapping = TextWrapping.Wrap,
						Text = text,
					},
					okButton,
				},
			}
		};

		void ClosePopup(object? sender, RoutedEventArgs e)
		{
			okButton.Click -= ClosePopup;
			backgroundWindow.Close();
		}
		void FocusOK(object? sender, EventArgs e)
		{
			backgroundWindow.Opened -= FocusOK;
			okButton.Focus();
		}

		okButton.Click += ClosePopup;
		backgroundWindow.Opened += FocusOK;
		await backgroundWindow.ShowDialog(this);
	}
}