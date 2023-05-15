using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FLP;
using Kermalis.MIDI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MIDIProgramSplitter.GUI;

internal sealed partial class MainWindow : Window
{
	private readonly FilePickerFileType _midiPickerType;
	private readonly FilePickerFileType _flpPickerType;
	private readonly FilePickerFileType _dlsPickerType;

	private readonly FLPSaveOptions _flpOptions;

	private Splitter? _splitter;

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
			IStorageFile storageFile = result[0];
			InputMIDI.Text = storageFile.Path.LocalPath;

			Stream fs = await storageFile.OpenReadAsync();
			var midi = new MIDIFile(fs);
			await fs.DisposeAsync();

			_flpOptions.DefaultMIDIVolume = (byte)DefaultMIDIVolume.Value!;

			_splitter = new Splitter(midi, _flpOptions.DefaultMIDIVolume); // TODO: MIDI import options
			LogControl.Items = _splitter.SLog;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex); // TODO: Popup
			InputMIDI.Text = string.Empty;
			LogControl.Items = new string[1] { ex.ToString() };
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

		IStorageFile storageFile = result[0];
		InputDLS.Text = storageFile.Path.LocalPath;
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
			SuggestedFileName = _splitter.ToString(), // TODO
		};

		IStorageFile? result = await StorageProvider.SaveFilePickerAsync(options);
		if (result is null)
		{
			return;
		}

		using (FileStream s = Create(result))
		{
			_splitter.SaveMIDI(s);
		}
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
			SuggestedFileName = _splitter.ToString(), // TODO
		};

		IStorageFile? result = await StorageProvider.SaveFilePickerAsync(options);
		if (result is null)
		{
			return;
		}

		UpdateFLPOptions();

		using (FileStream s = Create(result))
		{
			_splitter.SaveFLP(s, _flpOptions);
		}
	}

	private static FileStream Create(IStorageFile f)
	{
		return File.Create(f.Path.LocalPath);
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

		FLPPatternColorMode.SelectedIndex = (int)_flpOptions.PatternColorMode;
		FLPInsertColorMode.SelectedIndex = (int)_flpOptions.InsertColorMode;
		FLPAutomationColorMode.SelectedIndex = (int)_flpOptions.AutomationColorMode;
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

		_flpOptions.PatternColorMode = (FLPSaveOptions.EPatternColorMode)FLPPatternColorMode.SelectedIndex;
		_flpOptions.InsertColorMode = (FLPSaveOptions.EInsertColorMode)FLPInsertColorMode.SelectedIndex;
		_flpOptions.AutomationColorMode = (FLPSaveOptions.EAutomationColorMode)FLPAutomationColorMode.SelectedIndex;
	}
}