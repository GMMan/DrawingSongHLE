Game & Watch Mario Drawing Song HLE
===================================

This project allows you to play the Mario Drawing Song from the Game & Watch:
Super Mario Bros. on your computer. This is used for debugging any replacement
animations that you might be loading in. It is designed to run very similar
to how it's implemented on the Game & Watch. Some differences include decoding
animation frames in one update instead of several, using array indexing instead
of a running pointer, and some code reorganization. However, it should be
generally representative of what you'll find on the Game & Watch.

Getting started
---------------
Please put the decrypted external flash image as `external.bin` in the
`StreamingAssests` folder. If you are customizing your animation, you may want
to adjust the animation and audio length in `Config.cs`.

Controls
--------
- Arrow keys: select language. This is available while the animation is playing
  in addition to being available on the language menu.
- Enter: starts animation when on the language menu.
- Escape: pause/resume the animation.
- F5: reload all assets. Useful if you have changed `external.bin` while the
  program is still running.

Credits
-------
- IMA ADPCM decoder based on the one from https://github.com/jaames/game-and-watch-drawing-song-re/
- [ImageSharp](https://github.com/SixLabors/ImageSharp) for LZW decoding. See
  [patch](ImageSharp.patch) for changes made to expose LZE decoder.
