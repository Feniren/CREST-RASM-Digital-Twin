; NAME: Chamfer and part-off neck
; CATEGORY: Parting
; TOOLS: T1 = R0.8 roughing insert, T3 = R0.4 finishing insert, T4 = 2 mm parting insert
; STOCK: D32 x 70 mm bar, Z0 at the free face (chuck at Z-70), X is a diameter
; Turn Ø28 x 40, chamfer the front edge 2 x 45, then peck a parting neck at Z-34 down to Ø4.
; The neck stops short of the centre so the part stays on the bar for the demo.
%
N10 G21 G18 G40 G99
N20 T0101
N30 G50 S2500
N40 G96 S150 M03
N50 G00 X34 Z1
N60 X30
N70 G01 Z-40 F0.25
N80 X33
N90 G00 Z1
N100 X28
N110 G01 Z-40
N120 X31
N130 G00 Z1
N140 X60 Z30
N150 T0303
N160 G00 X20 Z1
N170 G01 Z0 F0.08
N180 X24
N190 X28 Z-2
N200 G00 X34
N210 Z1
N220 X60 Z30
N230 T0404
N240 G50 S1500
N250 G96 S100
N260 G00 X32 Z-34
N270 G01 X18 F0.04
N280 G00 U2
N290 G01 X10
N300 G00 U2
N310 G01 X4
N320 G04 X1
N330 G00 X34
N340 X60 Z30
N350 G28 U0 W0
N360 M05
N370 M30
