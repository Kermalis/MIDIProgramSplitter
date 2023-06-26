# MIDIProgramSplitter

A program that can split MIDI tracks with instrument changes without losing information!
There is a command-line version and a GUI version, [download here](https://github.com/Kermalis/MIDIProgramSplitter/releases)!

![GUI](https://github.com/Kermalis/MIDIProgramSplitter/assets/29823718/66314f49-7370-4aa1-92f8-5b182fde9422)


I created this program for two main uses:
* Wanting to make remixes of video game music.
There are always instrument changes on specific MIDI tracks which makes it a headache in a DAW.
So placing every instrument on a separate MIDI track means you can replace each individual one with a VST or something else!
* Wanting to import MIDI files into FL Studio without broken pitch bends.
This is a huge problem that Image-Line hasn't addressed for decades.
However, this program goes way beyond fixing the pitch bends.
It will create patterns, name things, and color everything, as well as automatically apply a .DLS to Fruity LSD if you want!
It is a way more powerful MIDI importer than the one built into FL Studio, and I hope they take notes.

When a MIDI track is split into multiple tracks, the events such as panpot and volume are kept for all of the new tracks.

Video of MIDI -> FLP (Outdated...):
* https://www.youtube.com/watch?v=gLra8-_D3WE
* https://www.youtube.com/watch?v=JawLuHkMT64

The videos don't show the automation clips at the bottom, and pitches were still broken in the FLP.
I'll hopefully upload an updated example soon that showcases all of the features...

If you are interested in how I'm reading/writing FLP files, you can just check out the code in the FLP folder.
It is its own library I wrote for this.

## MIDIProgramSplitter Uses:
* [Avalonia](https://github.com/AvaloniaUI/Avalonia)
* [EndianBinaryIO](https://github.com/Kermalis/EndianBinaryIO)
* [KFLP](https://github.com/Kermalis/KFLP)
* [KMIDI](https://github.com/Kermalis/KMIDI)