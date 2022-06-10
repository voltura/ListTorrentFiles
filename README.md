# MoveFinishedTorrentFiles

Command line tool I use to move finished qBitTorrent downloads; torrents that has not only been downloaded but seeded according to ratio rule setup in qBitTorrent and not seeding anymore but in state '__Finished__'.
The downloaded file(s)/director[y|ies] are moved to specified folder (via command line argument) or to a default folder.

Reading torrent information from qBitTorrent INI-file. 
Parsing file information from .torrent files themselves with help of Nuget package BencodeNET's BencodeParser.
