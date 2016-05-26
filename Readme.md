Axinom CPIX library
===================

This library is designed to implement a basic CPIX reader/writer scenario and to serve as an experiment platform to help define CPIX 2.0.

THIS IS HIGHLY EXPERIMENTAL AND NOT COMPATIBLE WITH ANY PUBLISHED VERSION OF CPIX

Platform compatibility
======================

This library is compatible with .NET Framework 4.6.0 and .NET Framework 4.6.1.

Features
========

The following features are implemented:

* Content key save/load
* Usage rule save/load
* Content key resolution based on usage rules
* Encryption of content keys (optional)
* Decryption of content keys
* Signing of content keys
* Signing of usage rules
* Signing of the document
* Automatic verification of all signatures
* Modification of existing document without having access to a decryption key
* Modification of existing document without invalidating signatures

The following features are NOT implemented:

* Key periods
* DRM system metadata
* Document update history