\ #include core.fs
-app
marker -app

D2 constant BUZZ
D3 constant FAN \ OC2B -- PWM
D6 constant RED
D7 constant GREEN

D4 constant PAD_CLK
D5 constant PAD_DATA
A0 A1 A2 A3 or or or constant PAD_IN


\ TCCR2A - Timer/Counter Control Register A
\   COM2A1 COM2A0 COM2B1 COM2B0 - - WGM21 WGM20
\ TCCR2B - Timer/Counter Control Register B
\   FOC2A FOC2B - - WGM22 CS22 CS21 CS20
\ WGM2=001  Phase correct PWM (TOP=$ff)
\ COM2B=10  Clear OC2B when counting up and equal to OCR2B
\ CS2=001   Clock enable (no prescaler)

\ Set FAN as PWM out on Timer 2, output B, at full frequency.
: fan-init
    FAN out
    %0010.0001 tccr2a c!
    %0000.0001 tccr2b c!
;
: fan-pow! ( c -- ) ocr2b c! ;  \ Set fan duty cycle.
: fan-pow? ( c -- ) ocr2b c@ ;

: pad-init
    PAD_CLK out
    PAD_DATA out
    PAD_IN in.high
    RED out
    GREEN out
    BUZZ out
;
: log2 ( +n1 -- n2 )
    2/ 0 begin
        over 0 > while
        1+ swap 2/ swap repeat
    nip
;
\ C1 low: %0111 A %1011 B %1101 C %1110 D
\ C2 low: %0111 3 %1011 6 %1101 9 %1110 #
\ C3 low: %0111 2 %1011 5 %1101 8 %1110 0
\ C4 low: %0111 1 %1011 4 %1101 7 %1110 *
\ Index by loop iter (3..0) | code log2
: pad-keys s" *7410852#963DCBA" ;
: pad-shift ( -- ) PAD_CLK dup high low ;
: pad-read ( -- c )
    \ Shifter controls C1..C4; input pins read R4..R1
    PAD_DATA high  3 for pad-shift next     \ Assuming unknown state, shift out 3 high bits.
    PAD_DATA low   pad-shift                \ Now shift the low bits we'll be testing.
    PAD_DATA high
    4 for
        pad-shift                           \ The shifter is buffered, so clock at least once.
        PAD_IN read %1111 xor               \ A0..A4 are all low bits; a pressed key will be a 1,2,4,8 value, or 0.
        dup 0 > if
            log2 r@ 4 * +                   \ Index into PAD_KEYS
            dup pad-keys rot ( i addr size i )
            > if + c@ rdrop exit then
        else drop
        then
    next
    0 \ Nothing pressed
;
: code-val s" 353A" ;
code-val swap drop constant code-len

create code-buf code-len allot
: code-clear code-buf code-len $00 fill ;
: code-in ( c - )  \ Shift code-buf left, write c at end.
    code-buf dup 1+ swap code-len 1- cmove
    code-buf code-len 1- + c!
;
: code-matched? ( -- b ) \ Compare code-in and code-val for equality.
    code-buf code-val for ( a-addr b-addr )
        over r@ + c@
        over r@ + c@
        <> if 2drop rdrop false exit then
    next
    2drop
    true
;

: init ( -- ) fan-init pad-init ;
: blink ( pin delay n -- ) 2* for over toggle dup ms next 2drop ;
: bad-code ( -- ) RED BUZZ or #100 3 blink ;
: good-code ( -- )
    GREEN BUZZ or #60 2 blink
    GREEN on
;

: control-loop ( -- )
    good-code
    $88 fan-pow!
    begin pad-read dup 0= if ( no key ) drop else
        GREEN BUZZ or #30 1 blink \ Signal the key press
        dup [char] # = if drop exit else
        dup [char] A = if drop fan-pow? $11 + $ff umin fan-pow! else
        dup [char] D = if drop fan-pow? $11 - 0 umax fan-pow! else
        drop then then then then
        #300 ms \ Debounce
    key? until
;
: reset code-clear $00 fan-pow!  GREEN off ;
: main ( -- )
    init
    begin pad-read dup 0= if drop  ( no key ) else
        RED BUZZ or #30 1 blink \ Signal the key press
        dup emit cr
        dup [char] # = if drop
            code-matched? if
                control-loop
            else bad-code then
            reset
        else code-in then
        #300 ms \ don't repeat key too quickly
    then
    key? until
;

' main is turnkey
