namespace MIDIProgramSplitter.FLP;

internal enum FLEvent : byte
{
	// BYTE EVENTS
	ChanEnabled = 0x00,
	NoteOn,
	ChanVol,
	ChanPan,
	MIDIChan,
	MIDINote,
	MIDIPatch,
	MIDIBank,
	// 8
	/// <summary>0 or 1</summary>
	LoopActive = 9,
	/// <summary>0 or 1</summary>
	ShowInfo,
	Shuffle,
	MainVol,
	FitToSteps,
	Pitchable,
	Zipped,
	DelayFlags,
	/// <summary>Value like 4</summary>
	TimeSigNumerator,
	/// <summary>Value like 4</summary>
	TimeSigDenominator,
	UseLoopPoints,
	LoopType,
	ChanType,
	TargetFXTrack,
	/// <summary>Value = 0</summary>
	PanVolumeTab,
	NStepsShown,
	SSLength,
	SSLoop,
	FXProps,
	IsRegistered,
	APDC,
	/// <summary>0 or 1</summary>
	TruncateClipNotes,
	EEAutoMode,
	Unk_32, // 0
	Unk_33, // 4
	Unk_34, // 4
	Unk_35, // 1
	Unk_36, // 0
	Unk_37, // 1
	Unk_38, // 1
	Unk_39, // 0
	Unk_40, // 0

	// WORD EVENTS
	NewChan = 0x40,
	NewPat,
	Tempo,
	/// <summary>Value like 0x100</summary>
	CurrentPatNum,
	PatData,
	FX,
	Fade_Stereo,
	CutOff,
	DotVol,
	DotPan,
	PreAmp,
	Decay,
	Attack,
	DotNote,
	DotPitch,
	DotMix,
	MainPitch,
	RandChan,
	MixChan,
	Resonance,
	OldSongLoopPos,
	StDel,
	FX3,
	DotReso,
	DotCutOff,
	ShiftDelay,
	LoopEndBar,
	Dot,
	DotShift,
	TempoFine,
	LayerChan,
	FXIcon,
	DotRel,
	SwingMix,
	Unk_98, // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9
	Unk_99, // 0
	Unk_100, // 0

	// DWORD EVENTS
	Color = 0x80,
	PlayListItem,
	Echo,
	FXSine,
	CutCutBy,
	WindowH,
	// 134
	MiddleNote = 135,
	/// <summary>May contain an invalid version info</summary>
	Reserved,
	MainResoCutOff,
	DelayReso,
	Reverb,
	StretchTime,
	SSNote,
	FineTune,
	SampleFlags,
	LayerFlags,
	ChanFilterNum,
	CurFilterNum,
	/// <summary>FX track output channel - 0xFFFFFFFF for none</summary>
	FXOutChanNum,
	/// <summary>+ Time and Mode in higher bits</summary>
	NewTimeMarker,
	FXColor,
	PatColor,
	PatAutoMode,
	SongLoopPos,
	AUSmpRate,
	/// <summary>FX track input channel - 0xFFFFFFFF for none</summary>
	FXInChanNum,
	PluginIcon,
	/// <summary>Value like 0xC0D40100</summary>
	FineTempo,
	Unk_157, // 0xFFFFFFFF
	Unk_158, // 0xFFFFFFFF
	Unk_159, // 2963
	Unk_164 = 164, // 0
	Unk_165, // 3
	Unk_166, // 1

	// TEXT EVENTS
	ChanName = 0xC0, // Name for the current channel
	PatName, // Name for the current pattern
	Title, // Title of the loop
	/// <summary>Old comments in text format. Not used anymore</summary>
	Comment,
	/// <summary>Filename for the sample in the current channel, stored as relative path</summary>
	SampleFileName,
	URL,
	/// <summary>New comments in Rich Text format</summary>
	CommentRTF,
	/// <summary>Text like "20.8.3.2304."</summary>
	Version,
	/// <summary>Text like "d3@?4xufs49p1n?B>;?889"</summary>
	RegistrationID,
	/// <summary>Plugin file name (without path)</summary>
	DefPluginName,
	ProjectDataPath,
	/// <summary>Plugin's name</summary>
	PluginName,
	FXName,
	/// <summary>Time marker name</summary>
	TimeMarker,
	Genre,
	Author,
	MIDICtrls,
	Delay,
	TS404Params,
	DelayLine,
	NewPlugin,
	PluginParams,
	/// <summary>Used once for testing</summary>
	Reserved2,
	/// <summary>Block of various channel params (can grow)</summary>
	ChanParams,
	/// <summary>Automated controller events</summary>
	CtrlRecChan,
	/// <summary>Selection in playlist</summary>
	PLSel,
	Envelope,
	BasicChanParams,
	OldFilterParams,
	ChanPoly,
	NoteEvents,
	AutomationData,
	PatternNotes,
	/// <summary>Init values for automated events</summary>
	InitCtrlRecChan,
	/// <summary>Remote control entry (MIDI)</summary>
	RemoteCtrl_MIDI,
	/// <summary>Remote control entry (internal)</summary>
	RemoteCtrl_Int,
	/// <summary>Vol/kb tracking</summary>
	Tracking,
	/// <summary>Levels offset</summary>
	ChanOfsLevels,
	/// <summary>Remote control entry formula</summary>
	RemoteCtrlFormula,
	/// <summary>Value like "Audio" or "Unsorted"</summary>
	ChanGroupName,
	RegBlackList,
	PlayListItems,
	/// <summary>Channel articulator</summary>
	ChanAC,
	FXRouting,
	FXParams,
	/// <summary>Value like: 10 DF D7 ED 3B A4 E5 40 00 00 00 E0 C9 BE 32 3F</summary>
	ProjectTime,
	PLTrackInfo,
	PLTrackName,
	Unk_241 = 241, // 24 len

	MAX
}
