; NAME: OD grooves
; CATEGORY: Grooving
; TOOLS: T1 = R0.8 roughing insert, T2 = 3 mm grooving insert
; STOCK: D32 x 70 mm bar, Z0 at the free face (chuck at Z-70), X is a diameter
; Turn Ø30 x 44, then four 3 mm grooves to Ø24 and one 6 mm groove (two plunges), each with a dwell.
; The grooving tool's programmed point is the centre of its 3 mm width. G97 = fixed rpm while grooving.
%
N10 G21 G18 G40 G99
N20 T0101
N30 G50 S2500
N40 G96 S150 M03
N50 G00 X34 Z1
N60 X30
N70 G01 Z-44 F0.25
N80 X33
N90 G00 Z1
N100 X60 Z30
N110 T0202
N120 G97 S800
N130 G00 X32 Z-8
N140 G01 X24 F0.05
N150 G04 X0.5
N160 G00 X32
N170 Z-16
N180 G01 X24
N190 G04 X0.5
N200 G00 X32
N210 Z-24
N220 G01 X24
N230 G04 X0.5
N240 G00 X32
N250 Z-32
N260 G01 X24
N270 G04 X0.5
N280 G00 X32
N290 Z-38.5
N300 G01 X26
N310 G00 X32
N320 W-3
N330 G01 X26
N340 G04 X0.5
N350 G00 X32
N360 X60 Z30
N370 G28 U0 W0
N380 M05
N390 M30
