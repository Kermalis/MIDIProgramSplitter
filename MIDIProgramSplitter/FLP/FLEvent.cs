namespace MIDIProgramSplitter.FLP;

internal enum FLEvent : byte
{
	// BYTE EVENTS
	ChannelIsEnabled = 0x00,
	NoteOn,
	ChannelVolume,
	ChannelPanpot,
	MIDIChan,
	MIDINote,
	MIDIPatch,
	MIDIBank,
	// 8
	/// <summary>0 for pattern mode, 1 for song mode</summary>
	IsSongMode = 9,
	/// <summary>0 or 1</summary>
	ShouldShowInfoOnOpen,
	Shuffle,
	MainVolume,
	FitToSteps,
	Pitchable,
	Zipped,
	DelayFlags,
	ProjectTimeSigNumerator,
	ProjectTimeSigDenominator,
	UseLoopPoints,
	ChannelLoopType,
	ChannelType,
	TargetFXTrack,
	/// <summary>0 for circular, 2 for triangular</summary>
	PanningLaw,
	NStepsShown,
	SSLength,
	SSLoop,
	FXProps,
	IsRegistered,
	APDC,
	ShouldPlayTruncatedClipNotes,
	EEAutoMode,
	Unk_32, // 0
	TimeSigMarkerNumerator,
	TimeSigMarkerDenominator,
	/// <summary>0 for original FL timing, 1 for traditional time signatures</summary>
	ProjectShouldUseTimeSignatures,
	Unk_36, // 0
	Unk_37, // 1
	Unk_38, // 1
	Unk_39, // 0
	ShouldCutNotesFast,

	// WORD EVENTS
	NewChannel = 0x40,
	NewPattern,
	Tempo,
	SelectedPatternNum,
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
	/// <summary>-1200 to 1200. 0 default</summary>
	MasterPitch,
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
	NewArrangement,
	CurArrangementNum,

	// DWORD EVENTS
	Color = 0x80,
	PlaylistItem,
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
	ChannelSampleFlags,
	ChannelLayerFlags,
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
	VersionBuildNumber,
	Unk_164 = 164, // 0
	Unk_165, // 3
	Unk_166, // 1

	// TEXT EVENTS
	ChannelName = 0xC0,
	PatternName,
	ProjectTitle,
	ProjectComment,
	/// <summary>Filename for the sample in the current channel, stored as relative path</summary>
	SampleFileName,
	ProjectURL,
	/// <summary>New comments in Rich Text format</summary>
	ProjectCommentRTF,
	Version,
	RegistrationID,
	/// <summary>Plugin file name (without path)</summary>
	DefPluginName,
	ProjectDataPath,
	/// <summary>Plugin's name</summary>
	PluginName,
	FXName,
	TimeMarkerName,
	ProjectGenre,
	ProjectAuthor,
	MIDICtrls,
	Delay,
	TS404Params,
	DelayLine,
	NewPlugin,
	PluginParams,
	/// <summary>Used once for testing</summary>
	Reserved2,
	ChannelParams,
	/// <summary>Automated controller events</summary>
	CtrlRecChan,
	/// <summary>Selection in playlist</summary>
	PlaylistSelection,
	ChannelEnvelope,
	BasicChanParams,
	OldFilterParams,
	ChanPoly,
	NoteEvents,
	PatternEvents,
	PatternNotes,
	/// <summary>Init values for automated events</summary>
	InitCtrlRecChan,
	MIDIInfo,
	AutomationConnection,
	/// <summary>Vol/kb tracking</summary>
	ChannelTracking,
	/// <summary>Levels offset</summary>
	ChanOfsLevels,
	/// <summary>Remote control entry formula</summary>
	RemoteCtrlFormula,
	ChanFilterName,
	RegBlackList,
	PlaylistItems,
	AutomationData,
	FXRouting,
	FXParams,
	/// <summary>Value like: 10 DF D7 ED 3B A4 E5 40 00 00 00 E0 C9 BE 32 3F</summary>
	ProjectTime,
	NewPlaylistTrack,
	PlaylistTrackName,
	PlaylistArrangementName = 241,

	MAX
}
