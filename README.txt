# Readme Oven Scheduling CLI
#notes #oven-scheduling 

## CLI is able to

- check solution validity/calculate objective values
- perform basic satisfiability check on instance (try to schedule every job individually)
- run greedy construction heuristic
- create warmstart data from initial solution for MiniZinc or CP Optimizer
- generate random instances 
- calculate problem-specific lower bounds 
- convert to .mzn/.dat format
- parse MiniZinc solution file
- calculate parameters of instance (input: instance in .json format, output .json with parameters)

## Literature

- Constraints
- CP
- Lower bounds PATAT?

## Installation/usage

-i                  (Default: ) Instance file

  -g                  (Default: false) Use Greedy Heuristic

  -s                  (Default: ) Solution file

  --mznS              (Default: ) Parse MiniZinc solution file, convert to
                      Output object and serialise to json. Path to instance file
                      (in json format) must be provided as well with option -i.

  --jSF               (Default: ) Filename of json file to which converted
                      MiniZinc solution should be serialised

  --nSer              (Default: false) Do not serialize instance and solution

  --wI                (Default: ) Warm start instance file

  --wS                (Default: ) Warm start solution file

  --wRepr             (Default: false) Boolean indicating whether the warm start
                      data is created for a minizinc model with a representative
                      job per batch

  -o                  (Default: ./) Output file location

  --valO              (Default: false) Validate output

  --logfile           (Default: logfile) Where to Store the output of the
                      solution validation (filename without extension)

  --wRT               (Default: 4) Weight used for the objective total oven
                      runtime

  --wST               (Default: 0) Weight used for the objective total setup
                      times

  --wSC               (Default: 1) Weight used for the objective total setup
                      costs

  --wT                (Default: 0) Weight used for the objective number of tardy
                      jobs

  -c                  (Default: false) Convert to MiniZinc instance

  --dznF              (Default: ) Filename without ending of minizinc data file
                      (dzn-format) or CPOptimizer data file (dat-format)

  --cCP               (Default: false) Convert to CPOptimizer instance

  --iCheck            (Default: false) Perform basic satisfiability check on
                      instance

  --iCFile            (Default: ) Path to file where result of basic
                      satisfiability check of instance should be stored

  -r                  (Default: 0) Generate random instance with this number of
                      jobs

  -m                  (Default: 2) Number of machines in random instance

  -a                  (Default: 2) Number of attributes in random instance

  --omt               (Default: 60) Maximum processing time for any job (in
                      minutes)

  --dt                (Default: 2) Number of different processing times among
                      which to choose

  --mt                (Default: false) Whether a maximum processing time should
                      be generated for every job or not. If not, max_time will
                      be equal to the overall maximum processing time.

  --ms                (Default: 5) Maximum size of a job

  --max_cap_l         (Default: 5) Lower bound for the maximum capacity of
                      machines

  --max_cap_u         (Default: 10) Upper bound for the maximum capacity of
                      machines

  --minSC             (Default: 1) Minimum number of shifts that are generated
                      per machine

  --maxSC             (Default: 2) Maximum number of shifts that are generated
                      per machine

  --avP               (Default: 0.5) Lower bound for the fraction of time that
                      every machine should be available. Between 0 and 1.

  --elP               (Default: 0.5) Probability of an additional machine to be
                      selected as eligible machine for a job (one machine will
                      always be selected). Between 0 and 1.

  --eSDf              (Default: 0.5) Factor used to create earliest start date.
                      Should be between 0 and 1. The larger the factor is, the
                      more the earliest start dates are spread out over the
                      scheduling horizon.

  --lEDf              (Default: 2) Factor used to create latest end date. Should
                      be larger or equal to 1. If equal to 1, all jobs must be
                      processed immediately. The larger the factor gets, the
                      more time there is for every job.

  --sC                (Default: none) Type of setup costs. Can be one of: none,
                      constant, arbitrary, realistic, symmetric.

  --sT                (Default: none) Type of setup times.  Can be one of: none,
                      constant, arbitrary, realistic, symmetric.

  --sgreedy           (Default: false) Boolean indicating whether instances that
                      cannot be solved by greedy heuristic should be thrown
                      away. (Ie, only instances where all jobs can be assigned
                      by greedy heuristic are accepted as random instances)

  --gRF               (Default: ) Filename without ending for the greedy
                      solution of a randomly created instance

  --lB                (Default: false) Calculate lower bounds for instance

  --lBF               (Default: ) Filename without ending for the calculated
                      lower bounds

  --iP                (Default: false) Calculate instance parameters

  --iPF               (Default: ) Filename without ending for the calculated
                      instance parameters

  --specialLexW       (Default: false) In MiniZinc converter (both for mzn and
                      CPOptimizer), create weights for UC2, ie, the special case
                      of lexicographic minimization with total oven runtime
                      lexicographically more important than tardiness,tardiness
                      lexicographically more important than setup costs.

  --valSpecialLexW    (Default: false) Do validation of solution with weights
                      for UC2, ie, the special case of lexicographic
                      minimization with total oven runtime lexicographically
                      more important than tardiness,tardiness lexicographically
                      more important than setup costs.

  --help              Display this help screen.

  --version           Display version information.



