QA40xPlot

This Audio Analysis application is a loose Fork of https://github.com/breedj/qa40x-audio-analyser, an excellent Windows Forms application
that interfaces with the QuantAsylum QA40x audio analyser.

The analyzer contains 

- a spectral plot
- an intermodulation distortion plot
- a frequency response analyzer
- a chart of THD vs Frequency 
- a chart of THD vs Amplitude.

The spectral and intermodulation tests include an option for autoranging. The THD vs xx plots autorange.

The code is based on C# and WPF (Windows Presentation Foundation).

Before starting this application, first start the QA40x program. Minimize it if you want, but it is used for the
REST interface. 

The Release is currently a .msi file that just needs to be executed. For now it is missing
the needed signature so the download has to be *kept*.

At startup the program looks in your My Documents folder and if it finds a file named
QADefault.cfg then that file is loaded.

![spectrum](QA40xPlot/Images/SpectralPlot.png)
![imd](QA40xPlot/Images/CCIFImdPlot.png)
![thd vs freq](QA40xPlot/Images/ThdVsFreq.png)


