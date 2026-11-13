; NAME: Face and step turn
; CATEGORY: Turning
; TOOLS: T1 = R0.8 roughing insert
; STOCK: D32 x 70 mm bar, Z0 at the free face (chuck at Z-70), X is a diameter
; ProTurn 9000, Fanuc-style lathe dialect: X diameter, U/W incremental, G99 F = mm/rev (default),
; G96 S = m/min constant surface speed clamped by G50 S. Face 0.5 mm, rough Ø26 x 40, step to Ø20 x 20.
%
N10 G21 G18 G40 G99
N20 T0101
N30 G50 S2500
N40 G96 S150 M03
N50 G00 X36 Z1
N60 Z-0.5
N70 G01 X-1 F0.15
N80 G00 Z1
N90 X30
N100 G01 Z-40 F0.25
N110 X33
N120 G00 Z1
N130 X28
N140 G01 Z-40
N150 X31
N160 G00 Z1
N170 X26
N180 G01 Z-40
N190 X29
N200 G00 Z1
N210 X24
N220 G01 Z-20
N230 X27
N240 G00 Z1
N250 X22
N260 G01 Z-20
N270 X25
N280 G00 Z1
N290 X20
N300 G01 Z-20
N310 X26
N320 G00 Z1
N330 X60 Z30
N340 G28 U0 W0
N350 M05
N360 M30
