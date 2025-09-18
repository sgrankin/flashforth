-ports
marker -ports

\ Pins are the combined (mapped) IO address and bit mask.
\ Multiple pins on the same port may be `or`d together to set/test them simultaneously.

: pin.pack ( mask addr -- addr.mask ) $100 * + ;
: pin.unpack ( addr.mask -- mask addr ) $100 u/mod ;

: low ( pin -- ) pin.unpack 1+ mclr ;       \ Set a pin low.
: high ( pin -- ) pin.unpack 1+ mset ;      \ Set a pin high.
: toggle ( pin -- ) pin.unpack 1- mset ;     \ Toggle output value.

: out ( pin -- ) pin.unpack mset ;          \ Set pin as output.
: in ( pin -- ) pin.unpack mclr ;           \ Set a pin as input
: in.high ( pin -- ) dup in high ;          \ Set pin as input_pullup.

: on high ;
: off low ;

: read ( pin -- f ) \ Read a pin, returns bit value if set.
    pin.unpack 1- mtst ;

\ Arduino Board Pins, referenced using Board Pins not AVR registers if possible
\ Pins referenced by nn, where nn is the Arduino board pin number

$20 ddrc pin.pack constant A5 \ Board Connector A5 PC5
$10 ddrc pin.pack constant A4 \ Board Connector A4 PC4
$08 ddrc pin.pack constant A3 \ Board Connector A3 PC3
$04 ddrc pin.pack constant A2 \ Board Connector A2 PC2
$02 ddrc pin.pack constant A1 \ Board Connector A1 PC1
$01 ddrc pin.pack constant A0 \ Board Connector A0 PC0

$20 ddrb pin.pack constant D13 \ Board Connector 13 PB5
$10 ddrb pin.pack constant D12 \ Board Connector 12 PB4
$08 ddrb pin.pack constant D11 \ Board Connector 11 PB3 PWM OC2A
$04 ddrb pin.pack constant D10 \ Board Connector 10 PB2 PWM OC1B
$02 ddrb pin.pack constant D9  \ Board Connector  9 PB1 PWM OC1A
$01 ddrb pin.pack constant D8  \ Board Connector  8 PB0

$80 ddrd pin.pack constant D7  \ Board Connector  7 PD7
$40 ddrd pin.pack constant D6  \ Board Connector  6 PD6 PWM OC0A
$20 ddrd pin.pack constant D5  \ Board Connector  5 PD5 PWM OC0B
$10 ddrd pin.pack constant D4  \ Board Connector  4 PD4
$08 ddrd pin.pack constant D3  \ Board Connector  3 PD3 PWM OC2B
$04 ddrd pin.pack constant D2  \ Board Connector  2 PD2
$02 ddrd pin.pack constant D1  \ Board Connector  1 PD1
$01 ddrd pin.pack constant D0  \ Board Connector  0 PD0

D13 constant LED

\ I/O memory references to port registers
$0005 constant portb-io \ IO-space address
$0003 constant pinb-io  \ IO-space address
$0009 constant pind-io  \ IO-space address

\ TODO : enable mset ;  \ enable an interrupt
\ TODO : disable mclr ; \ disable an interrupt

