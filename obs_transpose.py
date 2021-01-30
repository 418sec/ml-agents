import numpy as np
import itertools

one_obs = np.ones(2)
agent_buffer_field = [[one_obs, one_obs], [one_obs]]
# Should return a list of
# [np.ones((2, 2)), np.ones(2,2)] where they're padded

print(np.asanyarray(agent_buffer_field))

# Find the first observation. This should be USUALLY O(1)
for _team_obs in agent_buffer_field:
    if _team_obs:
        obs_shape = _team_obs[0].shape

new_list = list(
    map(lambda x: np.asanyarray(x), itertools.zip_longest(*agent_buffer_field, fillvalue=np.zeros(obs_shape)))
)
print(new_list)