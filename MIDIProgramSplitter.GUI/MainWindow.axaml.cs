using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
	private IStorageFile? _saveFLPFile;

	public MainWindow()
	{
		InitializeComponent();

		_flpOptions = new FLPSaveOptions();

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

			_splitter = new Splitter(midi);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex); // TODO: Popup
		}
	}
	private async void OnClickedBrowseOutputMIDI(object? sender, RoutedEventArgs e)
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

		OutputMIDI.Text = result.Path.LocalPath;
	}
	private async void OnClickedBrowseOutputFLP(object? sender, RoutedEventArgs e)
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

		_saveFLPFile = await StorageProvider.SaveFilePickerAsync(options);
		if (_saveFLPFile is null)
		{
			return;
		}

		OutputFLP.Text = _saveFLPFile.Path.LocalPath;
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
	private async void OnClickedSaveFLP(object? sender, RoutedEventArgs e)
	{
		if (_saveFLPFile is null || _splitter is null)
		{
			return;
		}

		_flpOptions.DLSPath = InputDLS.Text ?? string.Empty;

		using (Stream s = await _saveFLPFile.OpenWriteAsync())
		{
			_splitter.SaveFLP(s, _flpOptions);
		}
	}
}