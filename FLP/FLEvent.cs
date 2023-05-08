namespace FLP;

internal enum FLEvent : byte
{
	// 8bit
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
	/// <summary><see cref="FLChannelType"/></summary>
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
	/// <summary>FL21 - On every channel/automation/insert plugin. 1 for Channels/Automations, 0 for Inserts</summary>
	Unk_41,
	/// <summary>FL21 - On every insert</summary>
	Unk_42, // 0
	/// <summary>FL21 - On every playlist track</summary>
	Unk_43, // 0
	/// <summary>FL21</summary>
	PlaylistShouldUseAutoCrossfades,

	// 16bit
	NewChannel = 0x40,
	NewPattern,
	Tempo,
	SelectedPatternNum,
	PatData,
	FX,
	/// <summary><see cref="FLFadeStereo"/></summary>
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
	InsertIcon,
	DotRel,
	SwingMix,
	NewInsertSlot, // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9
	NewArrangement,
	CurArrangementNum,

	// 32bit
	PluginColor = 0x80,
	PlaylistItem,
	Echo,
	FXSine,
	/// <summary>Mixture of the "Cut" and "Cut By" numerics on a channel</summary>
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
	/// <summary>Insert track output channel - 0xFFFFFFFF for none</summary>
	InsertOutChanNum,
	/// <summary>+ Time and Mode in higher bits</summary>
	NewTimeMarker,
	InsertColor,
	PatternColor,
	PatternAutoMode,
	SongLoopPos,
	AUSmpRate,
	/// <summary>Insert track input channel - 0xFFFFFFFF for none</summary>
	InsertInChanNum,
	PluginIcon,
	/// <summary>Value like 0xC0D40100</summary>
	FineTempo,
	Unk_157, // 0xFFFFFFFF
	Unk_158, // 0xFFFFFFFF
	VersionBuildNumber,
	Unk_164 = 164, // 0
	Unk_165, // 3
	Unk_166, // 1

	// array
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
	InsertName,
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
	BasicChannelParams,
	OldFilterParams,
	ChanPoly,
	NoteEvents,
	PatternEvents,
	PatternNotes,
	MixerParams,
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
	InsertRouting,
	InsertParams,
	/// <summary>Value like: 10 DF D7 ED 3B A4 E5 40 00 00 00 E0 C9 BE 32 3F</summary>
	ProjectTime,
	NewPlaylistTrack,
	PlaylistTrackName,
	PlaylistArrangementName = 241,

	MAX
}
