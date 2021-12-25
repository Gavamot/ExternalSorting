using Domain;

namespace ExternalSorting;



public static  class Sorting
{
    public static class QuickSort
    {
        static int Partition(StringLine[] array, int low, int high)
        {
            //1. Select a pivot point.
            var pivot = array[high];

            int lowIndex = (low - 1);

            //2. Reorder the collection.
            for (int j = low; j < high; j++)
            {
                if (array[j] <= pivot)
                {
                    lowIndex++;

                    (array[lowIndex], array[j]) = (array[j], array[lowIndex]);
                }
            }

            (array[lowIndex + 1], array[high]) = (array[high], array[lowIndex + 1]);

            return lowIndex + 1;
        }

        static void InnerSort(StringLine[] array, int low, int high)
        {
            if (low < high)
            {
                int partitionIndex = Partition(array, low, high);

                //3. Recursively continue sorting the array
                InnerSort(array, low, partitionIndex - 1);
                InnerSort(array, partitionIndex + 1, high);
            }
        }

       public static void Sort(StringLine[] array) => InnerSort(array, 0, array.Length - 1);
    }

    public const int RUN = 32;
     
    // This function sorts array from left index to
    // to right index which is of size atmost RUN
    static void insertionSort(StringLine[] arr,
        int left, int right)
    {
        for (int i = left + 1; i <= right; i++)
        {
            var temp = arr[i];
            int j = i - 1;
            while (j >= left && arr[j] > temp)
            {
                arr[j+1] = arr[j];
                j--;
            }
            arr[j+1] = temp;
        }
    }
       
    // merge function merges the sorted runs
    static void merge(StringLine[] arr, int l,
                                   int m, int r)
    {
        // original array is broken in two parts
        // left and right array
        int len1 = m - l + 1, len2 = r - m;
        var left = new StringLine[len1];
        var right = new StringLine[len2];
        for (int x = 0; x < len1; x++)
            left[x] = arr[l + x];
        for (int x = 0; x < len2; x++)
            right[x] = arr[m + 1 + x];
       
        int i = 0;
        int j = 0;
        int k = l;
       
        // After comparing, we merge those two array
        // in larger sub array
        while (i < len1 && j < len2)
        {
            if (left[i] <= right[j])
            {
                arr[k] = left[i];
                i++;
            }
            else
            {
                arr[k] = right[j];
                j++;
            }
            k++;
        }
       
        // Copy remaining elements
        // of left, if any
        while (i < len1)
        {
            arr[k] = left[i];
            k++;
            i++;
        }
       
        // Copy remaining element
        // of right, if any
        while (j < len2)
        {
            arr[k] = right[j];
            k++;
            j++;
        }
    }
       
    // Iterative Timsort function to sort the
    // array[0...n-1] (similar to merge sort)
    public static void TimSort(StringLine[] arr)
    {
        var n = arr.Length;
        // Sort individual subarrays of size RUN
        for (int i = 0; i < n; i+=RUN)
            insertionSort(arr, i,
                         Math.Min((i+RUN-1), (n-1)));
       
        // Start merging from size RUN (or 32).
        // It will merge
        // to form size 64, then
        // 128, 256 and so on ....
        for (int size = RUN; size < n;
                                 size = 2*size)
        {
             
            // Pick starting point of
            // left sub array. We
            // are going to merge
            // arr[left..left+size-1]
            // and arr[left+size, left+2*size-1]
            // After every merge, we increase
            // left by 2*size
            for (int left = 0; left < n;
                                  left += 2*size)
            {
                 
                // Find ending point of left sub array
                // mid+1 is starting point of
                // right sub array
                int mid = left + size - 1;
                int right = Math.Min((left +
                                    2*size - 1), (n-1));
       
                // Merge sub array arr[left.....mid] &
                // arr[mid+1....right]
                  if(mid < right)
                    merge(arr, left, mid, right);
            }
        }
    }
    

    // https://www.geeksforgeeks.org/heap-sort/
    public static void HeapSort(StringLine[] arr)
    {
        int n = arr.Length;

        // Build heap (rearrange array)
        for (int i = n / 2 - 1; i >= 0; i--)
            Heapify(arr, n, i);

        // One by one extract an element from heap
        for (int i = n - 1; i > 0; i--) {
            // Move current root to end
            (arr[0], arr[i]) = (arr[i], arr[0]);

            // call max heapify on the reduced heap
            Heapify(arr, i, 0);
        }
    }

    // To heapify a subtree rooted with node i which is
    // an index in arr[]. n is size of heap
    static void Heapify(StringLine[] arr, int n, int i)
    {
        int largest = i; // Initialize largest as root
        int l = 2 * i + 1; // left = 2*i + 1
        int r = 2 * i + 2; // right = 2*i + 2

        // If left child is larger than root
        if (l < n && arr[l] > arr[largest])
            largest = l;

        // If right child is larger than largest so far
        if (r < n && arr[r] > arr[largest])
            largest = r;

        // If largest is not root
        if (largest != i) {
            (arr[i], arr[largest]) = (arr[largest], arr[i]);

            // Recursively heapify the affected sub-tree
            Heapify(arr, n, largest);
        }
    }
    
    //https://programm.top/c-sharp/algorithm/array-sort/merge-sort/
    // complexity = n * lon(n) 
    static void Merge(StringLine[] array, int lowIndex, int middleIndex, int highIndex)
    {
        var left = lowIndex;
        var right = middleIndex + 1;
        var tempArray = new StringLine[highIndex - lowIndex + 1];
        var index = 0;

        while ((left <= middleIndex) && (right <= highIndex))
        {
            if (array[left] < array[right])
            {
                tempArray[index] = array[left];
                left++;
            }
            else
            {
                tempArray[index] = array[right];
                right++;
            }

            index++;
        }

        for (var i = left; i <= middleIndex; i++)
        {
            tempArray[index] = array[i];
            index++;
        }

        for (var i = right; i <= highIndex; i++)
        {
            tempArray[index] = array[i];
            index++;
        }

        for (var i = 0; i < tempArray.Length; i++)
        {
            array[lowIndex + i] = tempArray[i];
        }
    }
    
    static void MergeSort(StringLine[] array, int lowIndex, int highIndex)
    {
        if (lowIndex < highIndex)
        {
            var middleIndex = (lowIndex + highIndex) / 2;
            MergeSort(array, lowIndex, middleIndex);
            MergeSort(array, middleIndex + 1, highIndex);
            Merge(array, lowIndex, middleIndex, highIndex);
        }
    }
    
    public static void  MergeSort(StringLine[] array)
    {
         MergeSort(array, 0, array.Length - 1);
    }
}