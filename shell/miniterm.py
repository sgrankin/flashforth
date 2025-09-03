#!/usr/bin/env python3

import argparse
import sys
import threading
import time
from serial.tools import miniterm
import serial


class SlowSerial(serial.Serial):
    # Multiple hacks to get flashforth not to drop bits on the floor:
    # - Wait after sending a char for 2ms
    # - Wait after sending an NL for 200ms
    # - Also block after that NL until we see an NL echoed back
    # - Process XON/XOFF chars directly, as they don't seem to be getting processed by the underlying terminal.
    #   Due to the many buffers present, including in the hardware, just flow control is not enough, however.

    PER_CHAR_TX_DELAY_S = 0.001
    PER_NL_TX_DELAY_S = 0.0

    def __init__(self, *args, **kwargs):
        self.xon = threading.Event()
        self.xon.set()
        self.nlecho = threading.Event()
        super().__init__(*args, **kwargs)

    def write(self, data):
        if len(data) == 0:
            return 0

        count = 0
        for c in data:
            self.xon.wait()
            if c == ord(b"\n"):
                self.nlecho.clear()
            if super().write(bytes([c])) is None:
                return count or None
            count += 1
            time.sleep(self.PER_CHAR_TX_DELAY_S)
            if c == ord(b"\n"):
                self.nlecho.wait()  # Wait for echo...
                time.sleep(self.PER_NL_TX_DELAY_S)
        return count

    def read(self, size):
        chars = super().read(size)
        if chars is None:
            return None
        for c in chars:
            if c == 0x13:  # XOFF
                self.xon.clear()
            elif c == 0x11:  # XON
                self.xon.set()
            elif c == ord(b"\n"):
                self.nlecho.set()
        return chars.translate(None, b"\x11\x13")


def main():
    parser = argparse.ArgumentParser(
        description="Miniterm - A simple terminal program for the serial port."
    )
    parser.add_argument(
        "port", nargs="?", help='serial port name (default "-" to show port list)'
    )
    parser.add_argument(
        "baudrate",
        nargs="?",
        type=int,
        help="(default %(default)s)",
        default=250000,
    )
    args = parser.parse_args()
    if args.port is None or args.port == "-":
        try:
            args.port = miniterm.ask_for_port()
        except KeyboardInterrupt:
            sys.stderr.write("\n")
            parser.error("user aborted and port is not given")
        else:
            if not args.port:
                parser.error("port is not given")

    com = SlowSerial(
        args.port,
        args.baudrate,
        exclusive=True,
        xonxoff=True,
    )

    term = miniterm.Miniterm(com, filters=["default"], eol="lf")
    term.set_rx_encoding("utf-8")
    term.set_tx_encoding("utf-8")
    sys.stderr.write(
        "--- Miniterm on {p.name}  {p.baudrate},{p.bytesize},{p.parity},{p.stopbits} ---\n".format(
            p=term.serial
        )
    )
    sys.stderr.write(
        "--- Quit: {} | Menu: {} | Help: {} followed by {} ---\n".format(
            miniterm.key_description(term.exit_character),
            miniterm.key_description(term.menu_character),
            miniterm.key_description(term.menu_character),
            miniterm.key_description("\x08"),
        )
    )
    term.start()
    try:
        term.join(True)
    except KeyboardInterrupt:
        pass
    term.join()
    term.close()


if __name__ == "__main__":
    sys.exit(main())
