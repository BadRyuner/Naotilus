# Naotilus
A library for analyzing NativeAOT builds.
It's very crooked and doesn't fit all cases.
But it's better than nothing.
For the moment it does:
- Finds R2R header
- Finds all R2R Sections
- Parses Metadata Section (AsmDefs, TypeDefs, FieldDefs, MethodDefs)
- Parses InvokeMap (Function pointers)
- Finds some MethodTables (very shitty, finds 2% of all)
- Finds all System.String instances.

Can be used as a lib, or export some of this data into a py script for Ghidra