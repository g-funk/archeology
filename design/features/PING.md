# Ping

Ping is a small feature that helps in locating the potential dig sites. When the player digs, if there are fragments closeby, the closest top tile on top of the fragment gives a signal. There can be fake pings sometimes.

* The signal: the tile brightens a bit and fades back to normal color quikly
* The further the fragment is, the less visible the effect is
* Depth is taken into account when determining the distance from dig
* The radius of ping is configurable
* The brightest value (some number value) is configurable
* The fade length is configurable (milliseconds)
* If the closes fragment tile is exposed, no ping effect
