The QA40xPlot application consists of 8 different audio tests that use a QA402 or QA403 audio analyzer from QuantAsylum.

# Graph Manipulation
Common to all tests are plots of values. You can change them easily by

- Click a plot and drag the mouse to move the plot center point around.
- Scroll the mouse wheel in a plot to zoom in or out
- Scroll the mouse wheel over a plot axis to zoom only that axis in or out
- Click inside the plot to freeze a cursor. Click again to unfreeze.

# Amplitude Definitions

All tests have a choice as to amplitude definition.

|Setting|Description|
|---------|-----------|
|__Input Voltage__|Interpret voltages as QA40x generator settings.|
|__Output Voltage__|Determine voltage to achieve the desired output. This always does a gain calculation first.|
|__Output Power__|Use the amplifier load setting (see Settings) to determine output voltage from power and then use gain to calculate the input voltage|

The spectral tests (Spectrum and Intermodulation) have manual or automatic attenuation. The other tests are all automatic to avoid QA40x overload in either channel.

The THD vs Frequency sweep calculates/uses one generator voltage that it then sweeps with. If you select an output amplitude that voltage
ensures the maximum output of the sweep is that setting. The Input Voltage setting uses that voltage for the sweep.

The THD vs Amplitude test calculates distortion at each swept amplitude and varies generator voltage accordingly. Attenuation may increase as output voltage increases.

All 3 response tests (Response, Gain, Impedance) use a single generator voltage just like
the THD vs Frequency test.

# Attenuation Settings
QA40x attenuation is important for test device integrity. QA40xPlot tries very hard to use enough
attenuation for each test. Attenuation is always calculated based on both input channels (left and right) of the QA40x. If you only plan to test one channel we
recommend you short the other QA40x input channel, which will guarantee that
attenuation math ignores the shorted channel.

# Cursor

All of the tests support a **cursor**. As you move your mouse in the plot a small box in the lower right 
shows the nearest data point. Some charts have a visual on-screen point, some don't.

# Spectrum Test

This test sends a single sine wave out from the generator and plots the received
frequency spectral data (value vs frequency). For a Sine wave this produces
a skinny spike at the sine frequency surrounded by background noise
and (usually harmonic) distortion spikes.

## Options

| option      | description |
|-------------|----------|
|Use Generator |	This lets you easily turn the sine wave on or off.
|Attenuation	|	Manually select the attenuation value or autorange.|
||When you click start, autorange will first test at 42 attenuation then adjust to the (almost) least possible|
|Generator 1| Select the frequency and signal voltage.|
||The Set DUT option lets you program an output value rather than just the generator setting|
|Sampling|We recommend 48K or 96K Rate and 64K or 128K Size. 1 or 2 Averages.|

## Plot Options

| option      | description |
|-------------|----------|
|Axis Settings|You may restrict the axis bounds but still can zoom the axis in/out with the mouse|
||On selection or typing of a value the window will refit.|
|Summary Data|Select this to show the Summary box in the upper left.|
|Harmonic Markers|This will automatically mark the harmonics (and fundamental)|
|Power Markers|This will check 50Hz and 60Hz peaks, then mark the ones that seem like your power peaks.|
|% - Y Axis|This will show % instead of db on the Y axis. Nice for seeing distortion percents.|
|% - Data Summary|Swap between percents and uV/dBV|

# Intermodulation Test

This test performs one of 6 standard intermodulation tests or it allows manual frequency/amplitude selection per generator.

## Options

|option|description|
|------- |----------------|
|Use Generator 1/2|Lets you quickly turn each frequency on or off.|
|Attenuation|See the description under spectrum.|
|Intermod Test|Pick a test and an amplitude (voltage or power).|
||For custom, select the two frequencies and amplitudes manually.|
|Generator|When you have a stock test selected the only option available will be Voltage (or power).|
|Samples|See the description under spectrum|

## Plot Options

| option      | description |
|-------------|----------|
|Axis Settings|You may restrict the axis bounds but still can zoom the axis in/out with the mouse|
||On selection or typing of a value the window will refit.|
|Summary Data|Select this to show the Summary box in the upper left.|
|Intermod Markers|This will mark the standard intermodulation peaks.|
|Power Markers|This will check 50Hz and 60Hz peaks, then mark the ones that seem like your power peaks.|
|% - Y Axis|This will show % instead of db on the Y axis. Nice for seeing distortion percents.|
|% - Data Summary|Swap between percents and uV/dBV|

# THD vs Frequency Test

This test does a sweep from start to end frequency. At each frequency of
the sweep it tests the harmonic distortion products and plots
the resultant set of 5-8 line segments.

## Options

|option|description|
|------- |----------------|
|Generator|See Spectrum|
|Sweep|Define the frequencies to be swept. The end frequency should be less than SampleRate/2.|
|Sampling|See Spectrum|


## Plot Options

| option      | description |
|-------------|----------|
|Axis Settings|You may restrict the axis bounds but still can zoom the axis in/out with the mouse|
|Graph Data|Individually turn on/off any of the harmonics|
||High order harmonics may be nonsense at high frequencies|

# THD vs Amplitude Test

This test does a sweep from start to end voltage or power. At each amplitude it tests the harmonic distortion products and plots
the resultant set of 5-8 line segments.

## Options

|option|description|
|------- |----------------|
|Generator|See Spectrum|
|Sweep|Define the amplitude range to be swept. At each test point (amplitude level) the attenuation is adjusted to as low as possible.||
|Sampling|See Spectrum|


## Plot Options

| option      | description |
|-------------|----------|
|Axis Settings|You may restrict the axis bounds but still can zoom the axis in/out with the mouse|
|Graph Data|Individually turn on/off any of the harmonics|
||High order harmonics may be nonsense at high frequencies|

# Response Test

This is a conventional frequency response test. The output is in dBV
and can cover both channels or just one.

# Impedance Test

This does a frequency sweep and at each point calculates the effective
impedance of the DUT. Data is stored as a complex value.

Connections are:
- DUT terminal A to ground
- DUT terminal B to reference resistor terminal A and Left Input
- reference resistor terminal B and generator output to Right Input

# Gain Test

This does a traditional Bode Plot of Gain and Phase vs frequency.
Here the right channel is used as a reference channel. Connect the
right channel input to the DUT input port and generator output.


