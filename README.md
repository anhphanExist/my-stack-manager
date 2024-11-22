# My stack manager

An implementation that use segmentation to virtualize internal buffer, 
makes each stack operate in isolation from the outside.


## Time complexity
Let n be the size of internal buffer
Number of segments can reach n but rarely and optimize further

Push: Amortized O(1)

Pop: Amortized O(1)

RequestNewStack : O(n)

ReturnStackBack: O(n log n)

## Future Optimization 

RequestNewStack:
Use a priority queue for faster allocation from the freeSegments list, 
reducing the complexity to O(logn).

MergeFreeSegments:
Maintain the freeSegments list in sorted order at all times , which could make merging more efficient. 
This approach avoids a separate sort during merging, reducing complexity to O(n)