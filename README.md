# ListTorrentFiles

Command line tool I use to move finished qBitTorrent downloads; torrents that has not only been downloaded but seeded according to ratio rule setup in qBittorrent and not seeding anymore but in state "Finished".
The downloaded file(s)/director[y|ies] are moved to specified folder (via command line argument) or to a default folder.

Reading torrent information from qBitTorrent INI-file and parsing file information from .torrent files themselves with help of Nuget package BencodeNET's BencodeParser.
