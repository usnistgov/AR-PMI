# AR-PMI

(work in progress)

This Unity project uses Vuforia model tracking to overlay Product Manufacturing Information (PMI) and QIF inspection data on the [NIST Box Assembly Dataset](https://smstestbed.nist.gov/tdp/mtc/). [[Demo video and files](https://pages.nist.gov/CAD-PMI-Testing/NIST-AR-video.html)]

Conference presentation of this work: https://www.youtube.com/watch?v=3DH9oubebmk

## Contents

### Scripts (Assets/Scripts/)

#### ReadX3D.cs

This script takes an X3D file as input, and renders the geometry and graphical PMI annotations in Unity. 
It is not a full-fledged X3D importer, rather it only handles X3D elements used in the output of the [NIST Step File Analyzer](https://www.nist.gov/services-resources/software/step-file-analyzer-and-viewer) (SFA).
The script preserves the view-based hierarchy structure encoded in X3D, generating each view and the part geometry as children of the game object it is attached to.
It should be attached to an empty game object and the geometry and views will be generated as children of this game object.

#### ReadQIF.cs

This script takes a QIF file as input, and parses a subset of the file.
If the script is attached to the same object as the ReadX3D.cs script, it tries to match X3D annotations to the measured characteristics inspection results from QIF.
New QIF annotations are generated, color coded and grouped based on the results (Passed - green, Failed - red and Inconclusive - yellow).

### Data (Assets/StreamingAssets/)

#### QIF

The latest version of the QIF files from the Box Assembly dataset, in the 2.1 and 3.0 version of QIF, as well as the FAIR statistics from Mitutoyo.

#### STEP

Original STEP files of the Box Assembly that were translated to X3D via SFA.

#### X3D

Latest version of X3D files created by SFA. Also has older version of the X3D files. It also contains new example files sent by Soonjo containing surface highlights (this functionality has been implemented in STP2X3D, but it is still being incorporated into SFA). 

## Usage

Open the project in Unity version 2020.X.

This project is the AR-ready version deployed on an android tablet. When running the project in the editor without a camera, it uses Vuforia's simulated environment. You will need to move with the w,a,s,d, q (down) and e (up) keys. The X3D geometries are drawn only when the Vuforia targets are detected in the camera view.

Alternatively, you can just use the ReadX3D.cs and ReadQIF.cs scripts in a new Unity project or scene (attach the scripts to an empty game object) to import everything. 

## Note

The surface highlight functionality is currently only availabe on the Teo-Dev branch.
