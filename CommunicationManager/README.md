# Serial port communication

Add a Communication Manager item to the HMI tree, select a serial port and baud rate, then add a mapping for each property to exchange. Every mapping accepts incoming values; enable **Send updates** to also transmit the property's current value on connect and each time it changes. Mappings are saved with the HMI document.

Messages are UTF-8 lines terminated by `\n` (a preceding `\r` is ignored), in this form:

```text
!external-name:arguments
```

The external name must match a mapping exactly. The argument after the first colon is parsed using the mapped property's type: invariant-culture integer or finite double, or `True`/`False` for boolean. A message without an argument is accepted but does not update the property. Outgoing values use the same format, with invariant-culture numbers and `True`/`False` booleans. Lines longer than 4096 characters are discarded.
