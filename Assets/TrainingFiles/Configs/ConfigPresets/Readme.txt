The rules of these presets is following:

If a value should be chosen randomly, you must write the range with brackets. Like: "myParam: [1, 10]"

If the values in the brackets have a decimal, it is interpreted as float instead of int.
When letters appear, the values in brackets will be interpreted as enum-names

If more than 2 numerical options in the brackets are preset, the middle one will be ignored. e.g. [1.0, 10.0] == [1.0, 2.0, 10.0]